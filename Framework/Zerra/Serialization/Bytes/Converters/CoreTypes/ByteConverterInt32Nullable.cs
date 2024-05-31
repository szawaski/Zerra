// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt32Nullable<TParent> : ByteConverter<TParent, int?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out int? value)
            => reader.TryReadInt32Nullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, int? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}