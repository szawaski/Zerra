// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterObject<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        private static readonly Stack<Dictionary<string, object?>> collectedValuesPool = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, object?> RentCollectedValues()
        {
#if NETSTANDARD2_0
            if (collectedValuesPool.Count > 0)
                return collectedValuesPool.Pop();
            return new();
#else
            if (collectedValuesPool.TryPop(out var collectedValues))
                return collectedValues;
            return new();
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnCollectedValues(Dictionary<string, object?> collectedValues)
        {
            collectedValues.Clear();
            collectedValuesPool.Push(collectedValues);
        }

        private readonly Dictionary<ushort, ByteConverterObjectMember> membersByIndex = new();
        private readonly Dictionary<ushort, ByteConverterObjectMember> membersByIndexIngoreAttributes = new();
        private readonly Dictionary<string, ByteConverterObjectMember> membersByName = new();
        private bool indexSizeUInt16Only;

        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        protected override void Setup()
        {
            var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?>>();
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                if (member.Attributes.Any(x => x is NonSerializedAttribute))
                    continue;
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?>(member, indexAttribute));
            }

            //Members by Index with Attributes
            foreach (var member in memberSets.Where(x => x.Item2 != null))
            {
                if (member.Item2?.Index > Byte.MaxValue - indexOffset)
                    indexSizeUInt16Only = true;
                if (member.Item2?.Index > UInt16.MaxValue - indexOffset)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var index = (ushort)(member.Item2!.Index + indexOffset);

                var detail = ByteConverterObjectMember.New(member.Item1);
                membersByIndex.Add(index, detail);
            }

            //Members by Index and Name
            var hasAttributes = membersByIndex.Count > 0;
            var orderIndex = 0;
            foreach (var member in memberSets)
            {
                if (member.Item2?.Index > Byte.MaxValue - indexOffset)
                    indexSizeUInt16Only = true;
                if (member.Item2?.Index > UInt16.MaxValue - indexOffset)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var index = (ushort)(orderIndex + indexOffset);

                var detail = ByteConverterObjectMember.New(member.Item1);
                if (!hasAttributes)
                    membersByIndex.Add(index, detail);
                membersByIndexIngoreAttributes.Add(index, detail);
                membersByName.Add(member.Item1.Name, detail);

                orderIndex++;
            }

            if (typeDetail.Type.IsValueType || !typeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in typeDetail.ConstructorDetails.OrderByDescending(x => x.ParametersInfo.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.ParametersInfo)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.ParameterType == typeDetail.Type || parameter.Name == null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!membersByName.Values.Any(x => x.Member.Type == parameter.ParameterType && String.Equals(x.Member.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;
                    this.parameterConstructor = constructor;
                    break;
                }
                this.collectValues = parameterConstructor != null;
            }
        }

        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            if (indexSizeUInt16Only && !state.IndexSizeUInt16 && !state.UsePropertyNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members for index size");

            Dictionary<string, object?>? collectedValues;

            if (state.Current.NullFlags && !state.Current.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }
            }

            if (!state.Current.HasCreated)
            {
                if (!state.Current.DrainBytes)
                {
                    if (collectValues)
                    {
                        value = default;
                        collectedValues = RentCollectedValues();
                    }
                    else if (typeDetail.HasCreator)
                    {
                        value = typeDetail.Creator();
                        collectedValues = null;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                    }
                }
                else
                {
                    value = default;
                    collectedValues = null;
                }

                state.Current.HasCreated = true;
            }
            else
            {
                if (collectValues)
                {
                    value = default;
                    collectedValues = (Dictionary<string, object?>)state.Current.Object!;
                }
                else
                {
                    value = (TValue?)state.Current.Object;
                    collectedValues = null;
                }
            }

            for (; ; )
            {
                ByteConverterObjectMember? property;
                if (!state.Current.HasReadProperty)
                {
                    if (state.UsePropertyNames)
                    {
                        int? stringLength;
                        if (!state.Current.StringLength.HasValue)
                        {
                            if (!reader.TryReadStringLength(false, out stringLength, out state.BytesNeeded))
                            {
                                state.Current.HasNullChecked = true;
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                        }
                        else
                        {
                            stringLength = state.Current.StringLength;
                            state.Current.StringLength = null;
                        }

                        if (stringLength!.Value == 0)
                        {
                            break;
                        }

                        if (!reader.TryReadString(stringLength.Value, out var name, out state.BytesNeeded))
                        {
                            state.Current.HasNullChecked = true;
                            state.Current.StringLength = stringLength;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }

                        property = null;
                        _ = membersByName?.TryGetValue(name!, out property);
                    }
                    else
                    {
                        ushort propertyIndex;
                        if (state.IndexSizeUInt16)
                        {
                            if (!reader.TryReadUInt16(out var propertyIndexValue, out state.BytesNeeded))
                            {
                                state.Current.HasNullChecked = true;
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                            propertyIndex = propertyIndexValue;
                        }
                        else
                        {
                            if (!reader.TryReadByte(out var propertyIndexValue, out state.BytesNeeded))
                            {
                                state.Current.HasNullChecked = true;
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                            propertyIndex = propertyIndexValue;
                        }

                        if (propertyIndex == endObjectFlagUShort)
                        {
                            break;
                        }

                        property = null;
                        if (state.IgnoreIndexAttribute)
                            _ = membersByIndexIngoreAttributes?.TryGetValue(propertyIndex, out property);
                        else
                            _ = membersByIndex?.TryGetValue(propertyIndex, out property);
                    }
                }
                else
                {
                    property = (ByteConverterObjectMember?)state.Current.Property;
                }

                if (property == null)
                {
                    if (!state.UsePropertyNames && !state.IncludePropertyTypes)
                        throw new Exception($"Cannot deserialize with property undefined and no types.");

                    //consume bytes but object does not have property
                    var converter = ByteConverterFactory<TValue>.GetTypeRequired();
                    state.PushFrame(false);
                    state.Current.DrainBytes = true;
                    var read = converter.TryReadFromParent(ref reader, ref state, default);
                    if (!read)
                    {
                        state.Current.HasNullChecked = true;
                        state.Current.HasReadProperty = true;
                        state.Current.Property = property;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }
                }
                else
                {
                    if (collectValues)
                    {
                        state.PushFrame(false);
                        var read = property.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues);
                        if (!read)
                        {
                            state.Current.HasNullChecked = true;
                            state.Current.HasReadProperty = true;
                            state.Current.Property = property;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }
                    else
                    {
                        state.PushFrame(false);
                        var read = property.Converter.TryReadFromParent(ref reader, ref state, value);
                        if (!read)
                        {
                            state.Current.HasNullChecked = true;
                            state.Current.HasReadProperty = true;
                            state.Current.Property = property;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }
                }

                state.Current.HasReadProperty = false;
            }

            if (collectValues)
            {
                var args = new object?[parameterConstructor!.ParametersInfo.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    if (collectedValues!.TryGetValue(parameterConstructor.ParametersInfo[i].Name!, out var parameter))
                        args[i] = parameter;
                }
                if (typeDetail.Type.IsValueType)
                {
                    value = (TValue?)parameterConstructor.CreatorBoxed(args);
                }
                else
                {
                    value = parameterConstructor.Creator(args);
                }

                ReturnCollectedValues(collectedValues!);
            }

            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue? value)
        {
            if (indexSizeUInt16Only && !state.IndexSizeUInt16 && !state.UsePropertyNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members for index size");

            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out state.BytesNeeded))
                {
                    return false;
                }
                state.Current.HasWrittenIsNull = true;
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>> enumerator;
            if (state.Current.Enumerator == null)
            {
                if (state.IgnoreIndexAttribute)
                    enumerator = membersByIndexIngoreAttributes.GetEnumerator();
                else
                    enumerator = membersByIndex.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>>)state.Current.Enumerator;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                var indexProperty = enumerator.Current;

                //TODO this hurts speed
                if (indexProperty.Value.IsNull(value))
                {
                    continue;
                }

                if (!state.Current.HasWrittenPropertyIndex)
                {
                    if (state.UsePropertyNames)
                    {
                        if (!writer.TryWrite(indexProperty.Value.Member.Name, false, out state.BytesNeeded))
                        {
                            state.Current.Enumerator = enumerator;
                            state.Current.EnumeratorInProgress = true;
                            return false;
                        }
                    }
                    else
                    {
                        if (state.IndexSizeUInt16)
                        {
                            if (!writer.TryWrite(indexProperty.Key, out state.BytesNeeded))
                            {
                                state.Current.Enumerator = enumerator;
                                state.Current.EnumeratorInProgress = true;
                                return false;
                            }
                        }
                        else
                        {
                            if (!writer.TryWrite((byte)indexProperty.Key, out state.BytesNeeded))
                            {
                                state.Current.Enumerator = enumerator;
                                state.Current.EnumeratorInProgress = true;
                                return false;
                            }
                        }
                    }
                }

                state.PushFrame(false);
                var write = indexProperty.Value.Converter.TryWriteFromParent(ref writer, ref state, value);
                if (!write)
                {
                    state.Current.Enumerator = enumerator;
                    state.Current.HasWrittenPropertyIndex = true;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }

                state.Current.HasWrittenPropertyIndex = false;
                state.Current.EnumeratorInProgress = false;
            }

            if (state.UsePropertyNames)
            {
                if (!writer.TryWrite(0, out state.BytesNeeded))
                {
                    state.Current.Enumerator = enumerator;
                    return false;
                }
            }
            else
            {
                if (state.IndexSizeUInt16)
                {
                    if (!writer.TryWrite(endObjectFlagUInt16, out state.BytesNeeded))
                    {
                        state.Current.Enumerator = enumerator;
                        return false;
                    }
                }
                else
                {
                    if (!writer.TryWrite(endObjectFlagByte, out state.BytesNeeded))
                    {
                        state.Current.Enumerator = enumerator;
                        return false;
                    }
                }
            }

            return true;
        }

        private abstract class ByteConverterObjectMember
        {
            public readonly MemberDetail Member;

            public abstract bool IsNull(TValue parent);

            public ByteConverterObjectMember(MemberDetail member)
            {
                this.Member = member;
            }

            private ByteConverter<TValue>? converter = null;
            public ByteConverter<TValue> Converter
            {
                get
                {
                    if (converter == null)
                    {
                        lock (this)
                        {
                            converter ??= ByteConverterFactory<TValue>.Get(Member.TypeDetail, Member.Name, Member.HasGetterBoxed ? Member.GetterTyped : null, Member.HasSetterBoxed ? Member.SetterTyped : null);
                        }
                    }
                    return converter;
                }
            }

            public abstract ByteConverter<Dictionary<string, object?>> ConverterSetCollectedValues { get; }

            //helps with debug
            public override string ToString()
            {
                return Member.Name;
            }

            private static readonly Type byteConverterObjectMemberT = typeof(ByteConverterObjectMember<>);
            public static ByteConverterObjectMember New(MemberDetail member)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), typeof(TValue), member.Type);
                var obj = generic.ConstructorDetails[0].CreatorBoxed(new object?[] { member });
                return (ByteConverterObjectMember)obj;
            }
        }

        private sealed class ByteConverterObjectMember<TValue2> : ByteConverterObjectMember
        {
            private readonly Func<TValue, TValue2?> getter;
            public override sealed bool IsNull(TValue parent) => getter(parent) is null;

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, TValue2? value) => parent.Add(Member.Name, value);
            public ByteConverterObjectMember(MemberDetail member)
                : base(member)
            {
                this.getter = ((MemberDetail<TValue, TValue2>)member).Getter;
                var type = TypeAnalyzer<Dictionary<string, object?>>.GetTypeDetail();
            }

            private ByteConverter<Dictionary<string, object?>>? converterSetValues;
            public override sealed ByteConverter<Dictionary<string, object?>> ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues == null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= ByteConverterFactory<Dictionary<string, object?>>.Get(Member.TypeDetail, Member.Name, null, SetterForConverterSetValues);
                        }
                    }
                    return converterSetValues;
                }
            }
        }
    }
}