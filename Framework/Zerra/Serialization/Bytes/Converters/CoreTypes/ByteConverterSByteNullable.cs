// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterSByteNullable<TParent> : ByteConverter<TParent, sbyte?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out sbyte? value)
            => reader.TryReadSByteNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, sbyte? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}