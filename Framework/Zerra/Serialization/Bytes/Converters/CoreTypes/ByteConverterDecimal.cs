// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterDecimal<TParent> : ByteConverter<TParent, decimal>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out decimal value)
            => reader.TryReadDecimal(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, decimal value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}