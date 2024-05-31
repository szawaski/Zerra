// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt16Nullable<TParent> : ByteConverter<TParent, ushort?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out ushort? value)
            => reader.TryReadUInt16Nullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, ushort? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}