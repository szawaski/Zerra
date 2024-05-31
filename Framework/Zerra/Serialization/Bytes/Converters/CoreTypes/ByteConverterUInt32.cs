// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt32<TParent> : ByteConverter<TParent, uint>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out uint value)
            => reader.TryReadUInt32(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, uint value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}