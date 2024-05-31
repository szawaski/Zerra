﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterType<TParent> : ByteConverter<TParent, Type?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out Type? value)
        {
            if (!state.Current.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.Current.NullFlags, out state.Current.StringLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }
                if (!state.Current.StringLength.HasValue || state.Current.StringLength.Value == 0)
                {
                    value = default;
                    return false;
                }
            }

            if (!reader.TryReadString(state.Current.StringLength.Value, out var typeName, out state.BytesNeeded))
            {
                value = default;
                return false;
            }
            if (typeName == null)
            {
                value = default;
                return true;
            }

            value = Discovery.GetTypeFromName(typeName);
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, Type? value)
            => writer.TryWrite(value?.FullName, state.Current.NullFlags, out state.BytesNeeded);
    }
}