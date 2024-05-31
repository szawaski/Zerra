// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterDecimalNullable<TParent> : ByteConverter<TParent, decimal?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out decimal? value)
        {
            if (!reader.TryReadDecimalNullable(state.Current.NullFlags, out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, decimal? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}