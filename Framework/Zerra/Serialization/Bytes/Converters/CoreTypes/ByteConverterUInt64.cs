// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt64<TParent> : ByteConverter<TParent, ulong>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out ulong value)
        {
            if (!reader.TryReadUInt64(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, ulong value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}