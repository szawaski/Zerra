// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateOnly<TParent> : ByteConverter<TParent, DateOnly>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateOnly value)
            => reader.TryReadDateOnly(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateOnly value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}

#endif