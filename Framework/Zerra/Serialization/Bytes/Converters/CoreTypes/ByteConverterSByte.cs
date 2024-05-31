// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterSByte<TParent> : ByteConverter<TParent, sbyte>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out sbyte value)
        {
            if (!reader.TryReadSByte(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, sbyte value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}