// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.IO;
using System;
using System.Linq;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTypeInfo<TParent> : ByteConverter<TParent>
    {
        private readonly ByteConverter<TParent>? converter;
        public ByteConverterTypeInfo(ByteConverter<TParent>? converter)
        {
            this.converter = converter;
        }

        public override void Read(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            var typeDetail = state.CurrentFrame.Converter.TypeDetail;

            if (options.IncludePropertyTypes)
            {
                int sizeNeeded;
                if (!state.CurrentFrame.StringLength.HasValue)
                {
                    if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.StringLength = stringLength;
                }

                if (!reader.TryReadString(state.CurrentFrame.StringLength!.Value, out var typeName, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return;
                }

                if (typeName == null)
                    throw new NotSupportedException("Cannot deserialize without type information");

                var typeFromBytes = Discovery.GetTypeFromName(typeName);

                if (typeFromBytes != null)
                {
                    var newTypeDetail = typeFromBytes.GetTypeDetail();

                    //overrides potentially boxed type with actual type if exists in assembly
                    if (typeDetail != null)
                    {
                        if (newTypeDetail.Type != typeDetail.Type && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                            throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");
                    }

                    var newConverter = ByteConverterFactory.Get<TParent>(options, newTypeDetail, memberDetail);
                    state.CurrentFrame.Converter = newConverter;
                    newConverter.Read(ref reader, ref state, parent);
                    return;
                }

                if (converter != null)
                {
                    state.CurrentFrame.Converter = converter;
                    converter.Read(ref reader, ref state, parent);
                    return;
                }
            }

            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();
                var newConverter = ByteConverterFactory.Get<TParent>(options, newTypeDetail, memberDetail);
                state.CurrentFrame.Converter = newConverter;
                newConverter.Read(ref reader, ref state, parent);
                return;
            }

            if (typeDetail == null || converter == null)
                throw new NotSupportedException("Cannot deserialize without type information");

            throw new NotSupportedException($"{nameof(ByteConverterTypeInfo<TParent>)} was not needed");
        }

        public override void Write(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            throw new NotImplementedException();
        }
    }
}