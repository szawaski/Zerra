// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterSingleNullable<TParent> : ByteConverter<TParent, float?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out float? value)
            => reader.TryReadSingleNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, float? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}