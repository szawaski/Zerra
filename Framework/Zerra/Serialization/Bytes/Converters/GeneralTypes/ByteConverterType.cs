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
        protected override bool Read(ref ByteReader reader, ref ReadState state, out Type? value)
        {
            int sizeNeeded;
            if (!state.CurrentFrame.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.CurrentFrame.NullFlags, out var stringLength, out sizeNeeded))
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
                state.CurrentFrame.StringLength = stringLength;
            }

            if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out var typeName, out sizeNeeded))
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

        protected override bool Write(ref ByteWriter writer, ref WriteState state, Type? value)
        {
            var valueType = state.CurrentFrame.Object == null ? null : (Type)state.CurrentFrame.Object;
            if (!writer.TryWrite(valueType?.FullName, state.CurrentFrame.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            };
            return true;
        }
    }
}