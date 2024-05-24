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
        public override bool Read(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (options.HasFlag(ByteConverterOptions.IncludePropertyTypes))
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

                if (!reader.TryReadString(state.CurrentFrame.StringLength!.Value, out var typeName, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }

                if (typeName == null)
                    throw new NotSupportedException("Cannot deserialize without type information");

                var typeFromBytes = Discovery.GetTypeFromName(typeName);

                var newTypeDetail = typeFromBytes.GetTypeDetail();

                //overrides potentially boxed type with actual type if exists in assembly
                if (typeDetail != null)
                {
                    if (newTypeDetail.Type != typeDetail.Type && !newTypeDetail.Interfaces.Contains(typeDetail.Type) && !newTypeDetail.BaseTypes.Contains(typeDetail.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.Type.GetNiceName()}");
                }

                var newConverter = ByteConverterFactory<TParent>.Get(options, newTypeDetail, null, getterBoxed, setterBoxed);
                state.CurrentFrame.Converter = newConverter;
                return newConverter.Read(ref reader, ref state, parent);
            }

            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();
                var newConverter = ByteConverterFactory<TParent>.Get(options, newTypeDetail, null, getterBoxed, setterBoxed);
                state.CurrentFrame.Converter = newConverter;
                return newConverter.Read(ref reader, ref state, parent);
            }

            throw new NotSupportedException($"{nameof(ByteConverterTypeInfo<TParent>)} should not have been used.");
        }

        public override bool Write(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            throw new NotImplementedException();
        }
    }
}