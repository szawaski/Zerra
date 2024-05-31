// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt16<TParent> : ByteConverter<TParent, ushort>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out ushort value)
        {
            if (!reader.TryReadUInt16(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, ushort value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}