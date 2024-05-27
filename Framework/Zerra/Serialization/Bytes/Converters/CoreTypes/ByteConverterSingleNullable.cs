// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterSingleNullable<TParent> : ByteConverter<TParent, float?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out float? value)
        {
            if (!reader.TryReadSingleNullable(state.Current.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, float? value)
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