// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.IO;
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
            var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>>();
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                var nonSerializedAttribute = member.Attributes.Select(x => x as NonSerializedAttribute).Where(x => x != null).FirstOrDefault();
                memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>(member, indexAttribute, nonSerializedAttribute));
            }

            //Members by Index with Ignore Attributes
            var membersWithIndexes = memberSets.Where(x => x.Item2 != null && x.Item3 == null).ToArray();
            if (membersWithIndexes.Length > 0)
            {
                foreach (var member in membersWithIndexes)
                {
                    if (member.Item2?.Index > Byte.MaxValue - indexOffset)
                        indexSizeUInt16Only = true;
                    if (member.Item2?.Index > UInt16.MaxValue - indexOffset)
                        throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                    var index = (ushort)(member.Item2!.Index + indexOffset);

                    var detail = ByteConverterObjectMember.New(member.Item1);
                    membersByIndexIngoreAttributes.Add(index, detail);
                }
            }

            //Members by Index and Name
            var orderIndex = 0;
            foreach (var member in memberSets.Where(x => x.Item3 == null))
            {
                if (member.Item2?.Index > Byte.MaxValue - indexOffset)
                    indexSizeUInt16Only = true;
                if (member.Item2?.Index > UInt16.MaxValue - indexOffset)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var index = (ushort)(orderIndex + indexOffset);

                var detail = ByteConverterObjectMember.New(member.Item1);
                membersByIndex.Add(index, detail);
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

            var frame = state.Current; //can change after enter another converter

            Dictionary<string, object?>? collectedValues;

            if (frame.NullFlags && !frame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out var sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }

                frame.HasNullChecked = true;
            }

            if (!frame.HasCreated)
            {
                if (!frame.DrainBytes)
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

                frame.HasCreated = true;
            }
            else
            {
                if (collectValues)
                {
                    value = default;
                    collectedValues = (Dictionary<string, object?>)frame.Object!;
                }
                else
                {
                    value = (TValue?)frame.Object;
                    collectedValues = null;
                }
            }

            for (; ; )
            {
                ByteConverterObjectMember? property;
                if (state.UsePropertyNames)
                {
                    int sizeNeeded;
                    if (!frame.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            if (collectValues)
                                frame.Object = collectedValues;
                            else
                                frame.Object = value;
                            return false;
                        }
                        frame.StringLength = stringLength;
                    }

                    if (frame.StringLength!.Value == 0)
                    {
                        break;
                    }

                    if (!reader.TryReadString(frame.StringLength.Value, out var name, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        if (collectValues)
                            frame.Object = collectedValues;
                        else
                            frame.Object = value;
                        return false;
                    }
                    frame.StringLength = null;

                    property = null;
                    _ = membersByName?.TryGetValue(name!, out property);

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !state.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory<TValue>.GetTypeRequired();
                        state.PushFrame(converter, false, value);
                        frame.DrainBytes = true;
                        var read = converter.TryReadFromParent(ref reader, ref state, value);
                        if (!read)
                        {
                            if (collectValues)
                                frame.Object = collectedValues;
                            else
                                frame.Object = value;
                            return false;
                        }
                    }
                    else
                    {
                        if (collectValues)
                        {
                            state.PushFrame(property.ConverterSetValues, false, collectedValues);
                            var read = property.ConverterSetValues.TryReadFromParent(ref reader, ref state, collectedValues);
                            if (!read)
                            {
                                if (collectValues)
                                    frame.Object = collectedValues;
                                else
                                    frame.Object = value;
                                return false;
                            }
                        }
                        else
                        {
                            state.PushFrame(property.Converter, false, value);
                            var read = property.Converter.TryReadFromParent(ref reader, ref state, value);
                            if (!read)
                            {
                                if (collectValues)
                                    frame.Object = collectedValues;
                                else
                                    frame.Object = value;
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    ushort propertyIndex;
                    if (state.IndexSizeUInt16)
                    {
                        if (!reader.TryReadUInt16(out var propertyIndexValue, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            if (collectValues)
                                frame.Object = collectedValues;
                            else
                                frame.Object = value;
                            return false;
                        }
                        propertyIndex = propertyIndexValue;
                    }
                    else
                    {
                        if (!reader.TryReadByte(out var propertyIndexValue, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            if (collectValues)
                                frame.Object = collectedValues;
                            else
                                frame.Object = value;
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

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !state.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory<TValue>.GetTypeRequired();
                        state.PushFrame(converter, false, default);
                        frame.DrainBytes = true;
                        var read = converter.TryReadFromParent(ref reader, ref state, default);
                        if (!read)
                        {
                            if (collectValues)
                                frame.Object = collectedValues;
                            else
                                frame.Object = value;
                            return false;
                        }
                    }
                    else
                    {
                        if (collectValues)
                        {
                            state.PushFrame(property.ConverterSetValues, false, collectedValues);
                            var read = property.ConverterSetValues.TryReadFromParent(ref reader, ref state, collectedValues);
                            if (!read)
                            {
                                if (collectValues)
                                    frame.Object = collectedValues;
                                else
                                    frame.Object = value;
                                return false;
                            }
                        }
                        else
                        {
                            state.PushFrame(property.Converter, false, value);
                            var read = property.Converter.TryReadFromParent(ref reader, ref state, value);
                            if (!read)
                            {
                                if (collectValues)
                                    frame.Object = collectedValues;
                                else
                                    frame.Object = value;
                                return false;
                            }
                        }
                    }
                }
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

            var frame = state.Current; //can change after enter another converter

            int sizeNeeded;
            if (frame.NullFlags && !frame.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                frame.HasWrittenIsNull = true;
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>> enumerator;
            if (frame.Enumerator == null)
            {
                if (state.IgnoreIndexAttribute)
                    enumerator = membersByIndexIngoreAttributes.GetEnumerator();
                else
                    enumerator = membersByIndex.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>>)frame.Enumerator;
            }

            while (frame.EnumeratorInProgress || enumerator.MoveNext())
            {
                var indexProperty = enumerator.Current;

                //TODO this hurts speed
                if (indexProperty.Value.IsNull(value))
                {
                    continue;
                }

                frame.EnumeratorInProgress = true;

                if (state.UsePropertyNames)
                {
                    if (!writer.TryWrite(indexProperty.Value.Member.Name, false, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        frame.Enumerator = enumerator;
                        return false;
                    }
                }
                else
                {
                    if (state.IndexSizeUInt16)
                    {
                        if (!writer.TryWrite(indexProperty.Key, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            frame.Enumerator = enumerator;
                            return false;
                        }
                    }
                    else
                    {
                        if (!writer.TryWrite((byte)indexProperty.Key, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            frame.Enumerator = enumerator;
                            return false;
                        }
                    }
                }

                frame.EnumeratorInProgress = false;

                state.PushFrame(indexProperty.Value.Converter, false, value);
                var write = indexProperty.Value.Converter.TryWriteFromParent(ref writer, ref state, value);
                if (!write)
                {
                    frame.Enumerator = enumerator;
                    return false;
                }
            }

            if (state.UsePropertyNames)
            {
                if (!writer.TryWrite(0, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    frame.Enumerator = enumerator;
                    return false;
                }
            }
            else
            {
                if (state.IndexSizeUInt16)
                {
                    if (!writer.TryWrite(endObjectFlagUInt16, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        frame.Enumerator = enumerator;
                        return false;
                    }
                }
                else
                {
                    if (!writer.TryWrite(endObjectFlagByte, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        frame.Enumerator = enumerator;
                        return false;
                    }
                }
            }

            return true;
        }

        private abstract class ByteConverterObjectMember
        {
            public readonly MemberDetail Member;

            public readonly ByteConverter<TValue> Converter;
            public ByteConverter<Dictionary<string, object?>> ConverterSetValues;
            public abstract bool IsNull(TValue parent);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public ByteConverterObjectMember(MemberDetail member)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            {
                this.Member = member;
                Converter = ByteConverterFactory<TValue>.Get(Member.TypeDetail, Member.Name, Member.GetterTyped, Member.SetterTyped);
            }

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
            private readonly Func<TValue, TValue2?> Getter;
            public override bool IsNull(TValue parent) => Getter(parent) == null;

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, TValue2? value) => parent.Add(Member.Name, value);
            public ByteConverterObjectMember(MemberDetail member)
                : base(member)
            {
                this.Getter = ((MemberDetail<TValue, TValue2>)member).Getter;
                var type = TypeAnalyzer<Dictionary<string, object?>>.GetTypeDetail();
                ConverterSetValues = ByteConverterFactory<Dictionary<string, object?>>.Get(Member.TypeDetail, Member.Name, null, SetterForConverterSetValues);
            }
        }
    }
}