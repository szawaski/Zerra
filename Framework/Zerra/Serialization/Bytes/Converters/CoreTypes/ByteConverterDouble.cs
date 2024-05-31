// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterDouble<TParent> : ByteConverter<TParent, double>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out double value)
            => reader.TryReadDouble(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, double value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}