// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterBoolean<TParent> : ByteConverter<TParent, bool>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out bool value)
        {
            if (!reader.TryReadBoolean(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, bool value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}