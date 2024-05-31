// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt16Nullable<TParent> : ByteConverter<TParent, ushort?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out ushort? value)
        {
            if (!reader.TryReadUInt16Nullable(state.Current.NullFlags, out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, ushort? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}