// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt32Nullable<TParent> : ByteConverter<TParent, uint?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out uint? value)
            => reader.TryReadUInt32Nullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, uint? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}