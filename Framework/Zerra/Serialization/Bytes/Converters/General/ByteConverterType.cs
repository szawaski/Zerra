// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterType<TParent> : ByteConverter<TParent, Type?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out Type? value)
        {
            int sizeNeeded;
            if (!state.Current.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.Current.NullFlags, out var stringLength, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }
                if (!stringLength.HasValue)
                {
                    value = default;
                    return false;
                }
                state.Current.StringLength = stringLength;
            }

            if (!reader.TryReadString(state.Current.StringLength.Value, out var typeName, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
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
        {
            if (!writer.TryWrite(value?.FullName, state.Current.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            };
            return true;
        }
    }
}