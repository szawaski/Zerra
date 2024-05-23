// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt64Nullable<TParent> : ByteConverter<TParent, ulong?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out ulong? value)
        {
            if (!reader.TryReadUInt64Nullable(state.CurrentFrame.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, ulong? value)
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