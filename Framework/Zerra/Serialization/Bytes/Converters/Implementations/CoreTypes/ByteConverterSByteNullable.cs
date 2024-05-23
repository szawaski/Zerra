// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterSByteNullable<TParent> : ByteConverter<TParent, sbyte?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out sbyte? value)
        {
            if (!reader.TryReadSByteNullable(state.CurrentFrame.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, sbyte? value)
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