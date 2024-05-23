// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt16Nullable<TParent> : ByteConverter<TParent, ushort?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out ushort? value)
        {
            if (!reader.TryReadUInt16Nullable(state.CurrentFrame.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, ushort? value)
        {
            if (!writer.TryWrite(value, state.CurrentFrame.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}