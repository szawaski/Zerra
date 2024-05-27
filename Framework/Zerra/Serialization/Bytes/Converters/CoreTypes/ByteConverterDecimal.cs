// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDecimal<TParent> : ByteConverter<TParent, decimal>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out decimal value)
        {
            if (!reader.TryReadDecimal(out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, decimal value)
        {
            if (!writer.TryWrite(value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}