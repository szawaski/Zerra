// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt16<TParent> : ByteConverter<TParent, short>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out short value)
            => reader.TryReadInt16(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, short value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}