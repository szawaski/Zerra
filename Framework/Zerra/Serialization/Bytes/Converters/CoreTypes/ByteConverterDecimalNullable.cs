// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterDecimalNullable<TParent> : ByteConverter<TParent, decimal?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out decimal? value)
            => reader.TryReadDecimalNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, decimal? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}