// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt16Nullable<TParent> : ByteConverter<TParent, ushort?>
    {
        protected override bool TryRead(ref ByteReader reader, ref ReadState state, out ushort? value)
        {
            if (!reader.TryReadUInt16Nullable(state.Current.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWrite(ref ByteWriter writer, ref WriteState state, ushort? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}