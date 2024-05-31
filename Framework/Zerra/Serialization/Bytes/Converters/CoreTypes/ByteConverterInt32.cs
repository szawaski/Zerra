// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt32<TParent> : ByteConverter<TParent, int>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out int value)
            => reader.TryReadInt32(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, int value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}