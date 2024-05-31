﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


namespace Zerra.Serialization
{
    internal sealed class ByteConverterInt64<TParent> : ByteConverter<TParent, long>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out long value)
            => reader.TryReadInt64(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, long value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}