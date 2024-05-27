// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt16Nullable<TParent> : ByteConverter<TParent, short?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out short? value)
        {
            if (!reader.TryReadInt16Nullable(state.Current.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, short? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}