// Copyright © KaKush LLC
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
            int? stringLength;
            if (!state.Current.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.Current.NullFlags, out stringLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }
                if (!stringLength.HasValue)
                {
                    value = default;
                    return false;
                }
            }
            else
            {
                stringLength = state.Current.StringLength;
            }

            if (!reader.TryReadString(stringLength.Value, out var typeName, out state.BytesNeeded))
            {
                state.Current.StringLength = stringLength;
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
            if (!writer.TryWrite(value?.FullName, state.Current.NullFlags, out state.BytesNeeded))
            {
                return false;
            };
            return true;
        }
    }
}