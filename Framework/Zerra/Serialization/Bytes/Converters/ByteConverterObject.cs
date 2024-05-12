// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterObject<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        private readonly Dictionary<ushort, ByteConverterMember<TValue>> propertiesByIndex = new();
        private readonly Dictionary<string, ByteConverterMember<TValue>> propertiesByName = new();

        public override void SetupAdditional()
        {
            var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>>();
            foreach (var member in TypeDetail.SerializableMemberDetails)
            {
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                var nonSerializedAttribute = member.Attributes.Select(x => x as NonSerializedAttribute).Where(x => x != null).FirstOrDefault();
                memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>(member, indexAttribute, nonSerializedAttribute));
            }

            if (!options.IgnoreIndexAttribute)
            {
                var membersWithIndexes = memberSets.Where(x => x.Item2 != null && x.Item3 == null).ToArray();
                if (membersWithIndexes.Length > 0)
                {
                    foreach (var member in membersWithIndexes)
                    {
                        switch (options.IndexSize)
                        {
                            case ByteSerializerIndexSize.Byte:
                                if (member.Item2!.Index > Byte.MaxValue - indexOffset)
                                    throw new Exception("Index attribute too large for the index size");
                                break;
                            case ByteSerializerIndexSize.UInt16:
                                if (member.Item2!.Index > UInt16.MaxValue - indexOffset)
                                    throw new Exception("Index attribute too large for the index size");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        var index = (ushort)(member.Item2.Index + indexOffset);

                        var detail = new ByteConverterMember<TValue>(options, member.Item1);
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
                    switch (options.IndexSize)
                    {
                        case ByteSerializerIndexSize.Byte:
                            if (orderIndex > Byte.MaxValue - indexOffset)
                                throw new Exception("Index attribute too large for the index size");
                            break;
                        case ByteSerializerIndexSize.UInt16:
                            if (orderIndex > UInt16.MaxValue - indexOffset)
                                throw new Exception("Index attribute too large for the index size");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    var index = (ushort)(orderIndex + indexOffset);

                    var detail = new ByteConverterMember<TValue>(options, member.Item1);
                    propertiesByIndex.Add(index, detail);
                    propertiesByName.Add(member.Item1.Name, detail);

                    orderIndex++;
                }
            }
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue? obj)
        {
            var nullFlags = state.CurrentFrame.NullFlags;

            if (nullFlags && !state.CurrentFrame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out var sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    obj = default;
                    return false;
                }

                if (isNull)
                {
                    state.EndFrame();
                    obj = default;
                    return false;
                }

                state.CurrentFrame.HasNullChecked = true;
            }

            if (!state.CurrentFrame.HasObjectStarted)
            {
                if (!state.CurrentFrame.DrainBytes && TypeDetail.HasCreator)
                    state.CurrentFrame.ResultObject = TypeDetail.Creator();
                else
                    state.CurrentFrame.ResultObject = null;
                state.CurrentFrame.HasObjectStarted = true;
            }

            obj = (TValue?)state.CurrentFrame.ResultObject;

            for (; ; )
            {
                //if (!state.CurrentFrame.DrainBytes && state.CurrentFrame.ObjectProperty != null)
                //{
                //    if (state.CurrentFrame.ResultObject != null)
                //        state.CurrentFrame.ObjectProperty.Setter(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                //    state.CurrentFrame.ObjectProperty = null;
                //}

                ByteConverterMember<TValue>? property;
                if (state.UsePropertyNames)
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
                        state.EndFrame();
                        return false;
                    }

                    if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var name, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.StringLength = null;

                    property = null;
                    _ = propertiesByName?.TryGetValue(name!, out property);
                    state.CurrentFrame.ObjectProperty = property;

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !options.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory.GetNeedTypeInfo<TValue>(options);
                        state.PushFrame(converter, false);
                        state.CurrentFrame.DrainBytes = true;
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        var converter = ByteConverterFactory.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
                        state.PushFrame(converter, false);
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                }
                else
                {
                    ushort propertyIndex;
                    switch (options.IndexSize)
                    {
                        case ByteSerializerIndexSize.Byte:
                            {
                                if (!reader.TryReadByte(out var value, out var sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return false;
                                }
                                propertyIndex = value;
                                break;
                            }
                        case ByteSerializerIndexSize.UInt16:
                            {
                                if (!reader.TryReadUInt16(out var value, out var sizeNeeded))
                                {
                                    state.BytesNeeded = sizeNeeded;
                                    return false;
                                }
                                propertyIndex = value;
                                break;
                            }
                        default: throw new NotImplementedException();
                    }

                    if (propertyIndex == endObjectFlagUShort)
                    {
                        //state.EndFrame();
                        return true;
                    }

                    property = null;
                    _ = propertiesByIndex?.TryGetValue(propertyIndex, out property);
                    state.CurrentFrame.ObjectProperty = property;

                    if (property == null)
                    {
                        if (!state.UsePropertyNames && !options.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        var converter = ByteConverterFactory.GetNeedTypeInfo<TValue>(options);
                        state.PushFrame(converter, false);
                        state.CurrentFrame.DrainBytes = true;
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        var converter = ByteConverterFactory.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
                        state.PushFrame(converter, false);
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                }
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue? obj)
        {
            throw new NotImplementedException();
        }
    }
}