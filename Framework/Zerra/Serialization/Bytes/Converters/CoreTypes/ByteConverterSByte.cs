// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterSByte<TParent> : ByteConverter<TParent, sbyte>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out sbyte value)
            => reader.TryReadSByte(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, sbyte value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}