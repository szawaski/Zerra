﻿// Copyright © KaKush LLC
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
        private readonly Dictionary<ushort, ByteConverterObjectMember> propertiesByIndex = new();
        private readonly Dictionary<string, ByteConverterObjectMember> propertiesByName = new();

        private bool usePropertyNames;
        private bool indexSizeUInt16;

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

                        var detail = new ByteConverterObjectMember(options, typeDetail, member.Item1);
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

                    var detail = new ByteConverterObjectMember(options, typeDetail, member.Item1);
                    propertiesByIndex.Add(index, detail);
                    propertiesByName.Add(member.Item1.Name, detail);

                    orderIndex++;
                }
            }

            this.usePropertyNames = options.HasFlag(ByteConverterOptions.UsePropertyNames);
            this.indexSizeUInt16 = options.HasFlag(ByteConverterOptions.IndexSizeUInt16);
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue? obj)
        {
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out var sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    obj = default;
                    return false;
                }

                if (isNull)
                {
                    obj = default;
                    return true;
                }

                state.CurrentFrame.HasNullChecked = true;
            }

            if (!state.CurrentFrame.HasObjectStarted)
            {
                if (!state.CurrentFrame.DrainBytes && typeDetail.HasCreator)
                    obj = typeDetail.Creator();
                else
                    obj = default;
                state.CurrentFrame.ResultObject = obj;
                state.CurrentFrame.HasObjectStarted = true;
            }
            else
            {
                obj = (TValue?)state.CurrentFrame.ResultObject;
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
                        return true;
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
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        var converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
                        state.PushFrame(converter, false);
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                }
                else
                {
                    ushort propertyIndex;
                    if (indexSizeUInt16)
                    {
                        if (!reader.TryReadUInt16(out var value, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        propertyIndex = value;
                    }
                    else
                    {
                        if (!reader.TryReadByte(out var value, out var sizeNeeded))
                        {
                            state.BytesNeeded = sizeNeeded;
                            return false;
                        }
                        propertyIndex = value;
                    }


                    if (propertyIndex == endObjectFlagUShort)
                    {
                        return true;
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
                        converter.Read(ref reader, ref state, obj);
                        if (state.BytesNeeded > 0)
                            return false;
                    }
                    else
                    {
                        var converter = ByteConverterFactory<TValue>.GetMayNeedTypeInfo(options, property.Member.TypeDetail, property.Converter);
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
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasWrittenIsNull)
            {
                if (obj == null)
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
                state.PushFrame(converter, false, obj);
                converter.Write(ref writer, ref state, obj);
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

        private sealed class ByteConverterObjectMember
        {
            private ByteConverterOptions options;
            private TypeDetail parent;
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

            //helps with debug
            public override string ToString()
            {
                return Member.Name;
            }
        }
    }
}