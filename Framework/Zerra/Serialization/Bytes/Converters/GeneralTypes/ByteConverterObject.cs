// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Collections;
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

        private readonly Dictionary<ushort, ByteConverterObjectMember> propertiesByIndex = new();
        private readonly Dictionary<string, ByteConverterObjectMember> propertiesByName = new();

        private bool usePropertyNames;
        private bool indexSizeUInt16;
        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        public override void Setup()
        {
            var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>>();
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                var nonSerializedAttribute = member.Attributes.Select(x => x as NonSerializedAttribute).Where(x => x != null).FirstOrDefault();
                memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>(member, indexAttribute, nonSerializedAttribute));
            }

            if (!options.HasFlag(ByteConverterOptions.IgnoreIndexAttribute))
            {
                var membersWithIndexes = memberSets.Where(x => x.Item2 != null && x.Item3 == null).ToArray();
                if (membersWithIndexes.Length > 0)
                {
                    foreach (var member in membersWithIndexes)
                    {
                        if (options.HasFlag(ByteConverterOptions.IndexSizeUInt16))
                        {
                            if (member.Item2!.Index > UInt16.MaxValue - indexOffset)
                                throw new Exception("Index attribute too large for the index size");
                        }
                        else
                        {
                            if (member.Item2!.Index > Byte.MaxValue - indexOffset)
                                throw new Exception("Index attribute too large for the index size");
                        }

                        var index = (ushort)(member.Item2.Index + indexOffset);

                        var detail = ByteConverterObjectMember.New(options, member.Item1);
                        propertiesByIndex.Add(index, detail);
                        propertiesByName.Add(member.Item1.Name, detail);
                    }
                }
            }

            if (propertiesByIndex.Count == 0)
            {
                var orderIndex = 0;
                foreach (var member in memberSets.Where(x => x.Item3 == null))
                {
                    if (options.HasFlag(ByteConverterOptions.IndexSizeUInt16))
                    {
                        if (member.Item2?.Index > UInt16.MaxValue - indexOffset)
                            throw new Exception("Index attribute too large for the index size");
                    }
                    else
                    {
                        if (member.Item2?.Index > Byte.MaxValue - indexOffset)
                            throw new Exception("Index attribute too large for the index size");
                    }

                    var index = (ushort)(orderIndex + indexOffset);

                    var detail = ByteConverterObjectMember.New(options, member.Item1);
                    propertiesByIndex.Add(index, detail);
                    propertiesByName.Add(member.Item1.Name, detail);

                    orderIndex++;
                }
            }

            this.usePropertyNames = options.HasFlag(ByteConverterOptions.UsePropertyNames);
            this.indexSizeUInt16 = options.HasFlag(ByteConverterOptions.IndexSizeUInt16);

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
                        if (!propertiesByName.Values.Any(x => x.Member.Type == parameter.ParameterType && String.Equals(x.Member.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)))
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

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            Dictionary<string, object?>? collectedValues;

            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasNullChecked)
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

                state.CurrentFrame.HasNullChecked = true;
            }

            if (!state.CurrentFrame.HasObjectStarted)
            {
                if (!state.CurrentFrame.DrainBytes)
                {
                    if (collectValues)
                    {
                        value = default;
                        collectedValues = RentCollectedValues();

                        state.CurrentFrame.ResultObject = collectedValues;
                    }
                    else if (typeDetail.HasCreator)
                    {
                        value = typeDetail.Creator();
                        collectedValues = null;
                        state.CurrentFrame.ResultObject = value;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                        state.CurrentFrame.ResultObject = value;
                    }
                }
                else
                {
                    value = default;
                    collectedValues = null;
                    state.CurrentFrame.ResultObject = value;
                }

                state.CurrentFrame.HasObjectStarted = true;
            }
            else
            {
                if (collectValues)
                {
                    value = default;
                    collectedValues = (Dictionary<string, object?>)state.CurrentFrame.ResultObject!;
                }
                else
                {
                    value = (TValue?)state.CurrentFrame.ResultObject;
                    collectedValues = null;
                }
            }

            for (; ; )
            {
                ByteConverterObjectMember? property;
                if (usePropertyNames)
                {
                    int sizeNeeded;
                    if (!state.CurrentFrame.StringLength.HasValue)
                    {
                        if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        state.CurrentFrame.StringLength = stringLength;
                    }

                    if (state.CurrentFrame.StringLength!.Value == 0)
                    {
                        break;
                    }

                    if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var name, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.StringLength = null;

                    property = null;
                    _ = propertiesByName?.TryGetValue(name!, out property);

                    if (property == null)
                    {
                        if (!usePropertyNames && !options.HasFlag(ByteConverterOptions.IncludePropertyTypes))
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory<TValue>.GetNeedTypeInfo(options);
                        state.PushFrame(converter, false, value);
                        state.CurrentFrame.DrainBytes = true;
                        var read = converter.Read(ref reader, ref state, value);
                        if (!read)
                            return false;
                    }
                    else
                    {
                        if (collectValues)
                        {
                            state.PushFrame(property.ConverterSetValues, false, collectedValues);
                            var read = property.ConverterSetValues.Read(ref reader, ref state, collectedValues);
                            if (!read)
                                return false;
                        }
                        else
                        {
                            state.PushFrame(property.Converter, false, value);
                            var read = property.Converter.Read(ref reader, ref state, value);
                            if (!read)
                                return false;
                        }
                    }
                }
                else
                {
                    ushort propertyIndex;
                    if (indexSizeUInt16)
                    {
                        if (!reader.TryReadUInt16(out var propertyIndexValue, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        propertyIndex = propertyIndexValue;
                    }
                    else
                    {
                        if (!reader.TryReadByte(out var propertyIndexValue, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        propertyIndex = propertyIndexValue;
                    }


                    if (propertyIndex == endObjectFlagUShort)
                    {
                        break;
                    }

                    property = null;
                    _ = propertiesByIndex?.TryGetValue(propertyIndex, out property);

                    if (property == null)
                    {
                        if (!usePropertyNames && !options.HasFlag(ByteConverterOptions.IncludePropertyTypes))
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory<TValue>.GetNeedTypeInfo(options);
                        state.PushFrame(converter, false, default);
                        state.CurrentFrame.DrainBytes = true;
                        var read = converter.Read(ref reader, ref state, default);
                        if (!read)
                            return false;
                    }
                    else
                    {
                        if (collectValues)
                        {
                            state.PushFrame(property.ConverterSetValues, false, collectedValues);
                            var read = property.ConverterSetValues.Read(ref reader, ref state, collectedValues);
                            if (!read)
                                return false;
                        }
                        else
                        {
                            state.PushFrame(property.Converter, false, value);
                            var read = property.Converter.Read(ref reader, ref state, value);
                            if (!read)
                                return false;
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

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasWrittenIsNull)
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
                state.CurrentFrame.HasWrittenIsNull = true;
            }

            IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>> enumerator;
            if (state.CurrentFrame.Enumerator == null)
            {
                enumerator = propertiesByIndex.GetEnumerator();
                state.CurrentFrame.Enumerator = enumerator;
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>>)state.CurrentFrame.Enumerator;
            }

            while (state.CurrentFrame.EnumeratorInProgress || enumerator.MoveNext())
            {
                var indexProperty = enumerator.Current;
                state.CurrentFrame.EnumeratorInProgress = true;

                if (usePropertyNames)
                {
                    if (!writer.TryWrite(indexProperty.Value.Member.Name, false, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                }
                else
                {
                    if (indexSizeUInt16)
                    {
                        if (!writer.TryWrite(indexProperty.Key, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                    }
                    else
                    {
                        if (!writer.TryWrite((byte)indexProperty.Key, out sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                    }
                }

                state.CurrentFrame.EnumeratorInProgress = false;

                state.PushFrame(indexProperty.Value.Converter, false, value);
                var write = indexProperty.Value.Converter.Write(ref writer, ref state, value);
                if (!write)
                    return false;
            }

            if (usePropertyNames)
            {
                if (!writer.TryWrite(0, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
            }
            else
            {
                if (indexSizeUInt16)
                {
                    if (!writer.TryWrite(endObjectFlagUInt16, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                }
                else
                {
                    if (!writer.TryWrite(endObjectFlagByte, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                }
            }

            return true;
        }

        private abstract class ByteConverterObjectMember
        {
            protected ByteConverterOptions options;
            public MemberDetail Member { get; private set; }

            public ByteConverterObjectMember(ByteConverterOptions options, MemberDetail member)
            {
                this.options = options;
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
                            if (converter == null)
                            {
                                var rootConverter = ByteConverterFactory<TValue>.Get(options, Member.TypeDetail, Member.Name, Member.GetterTyped, Member.SetterTyped);
                                converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, Member.TypeDetail, rootConverter);
                            }
                        }
                    }
                    return converter;
                }
            }

            public abstract ByteConverter<Dictionary<string, object?>> ConverterSetValues { get; }

            //helps with debug
            public override string ToString()
            {
                return Member.Name;
            }

            private static readonly Type byteConverterObjectMemberT = typeof(ByteConverterObjectMember<>);
            public static ByteConverterObjectMember New(ByteConverterOptions options, MemberDetail member)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), typeof(TValue), member.Type);
                var obj = generic.ConstructorDetails[0].CreatorBoxed(new object?[] { options, member });
                return (ByteConverterObjectMember)obj;
            }
        }

        private sealed class ByteConverterObjectMember<TValue2> : ByteConverterObjectMember
        {
            public ByteConverterObjectMember(ByteConverterOptions options, MemberDetail member)
                : base(options, member)
            {

            }

            private ByteConverter<Dictionary<string, object?>>? converterSetValues = null;
            public override ByteConverter<Dictionary<string, object?>> ConverterSetValues
            {
                get
                {
                    if (converterSetValues == null)
                    {
                        lock (this)
                        {
                            if (converterSetValues == null)
                            {
                                var type = TypeAnalyzer<Dictionary<string, object?>>.GetTypeDetail();
                                Action<Dictionary<string, object?>, TValue2?> setter = (parent, value) => parent.Add(Member.Name, value);
                                var rootConverter = ByteConverterFactory<Dictionary<string, object?>>.Get(options, Member.TypeDetail, Member.Name, null, setter);
                                converterSetValues = ByteConverterFactory<Dictionary<string, object?>>.GetMayNeedTypeInfo(options, Member.TypeDetail, rootConverter);
                            }
                        }
                    }
                    return converterSetValues;
                }
            }
        }
    }
}