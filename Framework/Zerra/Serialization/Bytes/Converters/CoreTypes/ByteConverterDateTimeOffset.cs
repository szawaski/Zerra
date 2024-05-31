// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTimeOffset<TParent> : ByteConverter<TParent, DateTimeOffset>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTimeOffset value)
        {
            if (!reader.TryReadDateTimeOffset(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTimeOffset value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}