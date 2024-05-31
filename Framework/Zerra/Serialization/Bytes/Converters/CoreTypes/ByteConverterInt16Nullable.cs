// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt16Nullable<TParent> : ByteConverter<TParent, short?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out short? value)
            => reader.TryReadInt16Nullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, short? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}