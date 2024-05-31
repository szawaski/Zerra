// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTime<TParent> : ByteConverter<TParent, DateTime>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTime value)
        {
            if (!reader.TryReadDateTime(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTime value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}