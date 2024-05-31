// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt32Nullable<TParent> : ByteConverter<TParent, int?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out int? value)
        {
            if (!reader.TryReadInt32Nullable(state.Current.NullFlags, out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, int? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}