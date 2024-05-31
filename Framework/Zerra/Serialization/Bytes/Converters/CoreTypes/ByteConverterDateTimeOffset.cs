// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTimeOffset<TParent> : ByteConverter<TParent, DateTimeOffset>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTimeOffset value)
            => reader.TryReadDateTimeOffset(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTimeOffset value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}