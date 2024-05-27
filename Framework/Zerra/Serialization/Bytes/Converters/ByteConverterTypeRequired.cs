﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTypeRequired<TParent> : ByteConverter<TParent>
    {
        private TypeDetail? typeDetail;
        private string? memberKey;
        private Delegate? getterDelegate;
        private Delegate? setterDelegate;

        private bool useEmptyImplementation;

        public override void Setup(TypeDetail? typeDetail, string? memberKey, Delegate? getterBoxed, Delegate? setterBoxed)
        {
            this.typeDetail = typeDetail;
            this.memberKey = memberKey;
            this.getterDelegate = getterBoxed;
            this.setterDelegate = setterBoxed;

            this.useEmptyImplementation = typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric;
        }

        public override bool TryReadValueObject(ref ByteReader reader, ref ReadState state, out object? value)
            => throw new InvalidOperationException();
        public override bool TryWriteValueObject(ref ByteWriter writer, ref WriteState state, object? value)
            => throw new InvalidOperationException();

        public override bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (state.IncludePropertyTypes)
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

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getterDelegate, setterDelegate);
                state.Current.StringLength = null;
                state.Current.Converter = newConverter;
                state.Current.HasTypeRead = true;
                return newConverter.TryReadFromParent(ref reader, ref state, parent);
            }

            if (useEmptyImplementation)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail!.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = ByteConverterFactory<TParent>.Get(newTypeDetail, memberKey, getterDelegate, setterDelegate);
                state.Current.Converter = newConverter;
                state.Current.HasTypeRead = true;
                return newConverter.TryReadFromParent(ref reader, ref state, parent);
            }

            throw new InvalidOperationException($"{nameof(ByteConverterTypeRequired<TParent>)} should not have been used.");
        }

        public override bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            throw new InvalidOperationException();
        }
    }
}