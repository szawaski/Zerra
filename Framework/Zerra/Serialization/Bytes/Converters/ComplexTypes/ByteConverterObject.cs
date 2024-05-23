// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterObject<TParent, TValue> : ByteConverter<TParent, TValue>
    {
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

                        var detail = ByteConverterObjectMember.New(options, typeDetail, member.Item1);
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

                    var detail = ByteConverterObjectMember.New(options, typeDetail, member.Item1);
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
                        collectedValues = new();

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
                        state.PushFrame(converter, false);
                        state.CurrentFrame.DrainBytes = true;
                        converter.Read(ref reader, ref state, value);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        var converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
                        state.PushFrame(converter, false);
                        converter.Read(ref reader, ref state, value);
                        if (state.BytesNeeded > 0)
                            return false;
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
                        state.PushFrame(converter, false);
                        state.CurrentFrame.DrainBytes = true;
                        converter.Read(ref reader, ref state, default);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        if (collectValues)
                        {
                            var converter = ByteConverterFactory<Dictionary<string, object?>>.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.ConverterSetValues);
                            state.PushFrame(converter, false);
                            converter.Read(ref reader, ref state, collectedValues);
                            if (state.BytesNeeded > 0)
                                return false;
                        }
                        else
                        {
                            var converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
                            state.PushFrame(converter, false);
                            converter.Read(ref reader, ref state, value);
                            if (state.BytesNeeded > 0)
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

                var converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, indexProperty.Value.Member.TypeDetail, indexProperty.Value.Converter);
                state.PushFrame(converter, false, value);
                converter.Write(ref writer, ref state, value);
                if (state.BytesNeeded > 0)
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
            protected TypeDetail parent;
            public MemberDetail Member { get; private set; }

            public ByteConverterObjectMember(ByteConverterOptions options, TypeDetail parent, MemberDetail member)
            {
                this.options = options;
                this.parent = parent;
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
                            converter ??= ByteConverterFactory<TValue>.Get(options, Member.TypeDetail, parent, Member.GetterTyped, Member.SetterTyped);
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
            public static ByteConverterObjectMember New(ByteConverterOptions options, TypeDetail parent, MemberDetail member)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), parent.Type, member.Type);
                var obj = generic.ConstructorDetails[0].CreatorBoxed(new object?[] { options, parent, member });
                return (ByteConverterObjectMember)obj;
            }
        }

        private sealed class ByteConverterObjectMember<TValue2> : ByteConverterObjectMember
        {
            public ByteConverterObjectMember(ByteConverterOptions options, TypeDetail parent, MemberDetail member)
                : base(options, parent, member)
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
                            var type = TypeAnalyzer<Dictionary<string, object?>>.GetTypeDetail();
                            Action<Dictionary<string, object?>, TValue2?> setter = (parent, value) => parent.Add(Member.Name, value);
                            converterSetValues ??= ByteConverterFactory<Dictionary<string, object?>>.Get(options, Member.TypeDetail, parent, null, setter);
                        }
                    }
                    return converterSetValues;
                }
            }
        }
    }
}