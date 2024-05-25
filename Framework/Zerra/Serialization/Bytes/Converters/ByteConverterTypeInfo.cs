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
        private TypeDetail? typeDetail;
        private Delegate? getterBoxed;
        private Delegate? setterBoxed;

        protected override void Setup(TypeDetail? typeDetail, Delegate? getterBoxed, Delegate? setterBoxed)
        {
            this.typeDetail = typeDetail;
            this.getterBoxed = getterBoxed;
            this.setterBoxed = setterBoxed;
        }

        public override bool TryRead(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (options.HasFlag(ByteConverterOptions.IncludePropertyTypes))
            {
                int sizeNeeded;
                if (!state.Current.StringLength.HasValue)
                {
                    if (!reader.TryReadStringLength(false, out var stringLength, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    state.Current.StringLength = stringLength;
                }

                if (!reader.TryReadString(state.Current.StringLength!.Value, out var typeName, out sizeNeeded))
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

                var newConverter = ByteConverterFactory<TParent>.GetAfterTypeInfo(options, newTypeDetail, memberKey, getterBoxed, setterBoxed);
                state.Current.StringLength = null;
                state.Current.Converter = newConverter;
                return newConverter.TryRead(ref reader, ref state, parent);
            }

            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();
                var newConverter = ByteConverterFactory<TParent>.GetAfterTypeInfo(options, newTypeDetail, memberKey, getterBoxed, setterBoxed);
                state.Current.Converter = newConverter;
                return newConverter.TryRead(ref reader, ref state, parent);
            }

            throw new InvalidOperationException($"{nameof(ByteConverterTypeInfo<TParent>)} should not have been used.");
        }

        public override bool TryWrite(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            throw new NotImplementedException();
        }
    }
}