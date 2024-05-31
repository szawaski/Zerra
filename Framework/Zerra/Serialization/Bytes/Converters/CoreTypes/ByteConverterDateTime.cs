// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTime<TParent> : ByteConverter<TParent, DateTime>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTime value)
            => reader.TryReadDateTime(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTime value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}