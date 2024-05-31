// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTimeSpanOffset<TParent> : ByteConverter<TParent, TimeSpan>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeSpan value)
        {
            if (!reader.TryReadTimeSpan(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeSpan value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}