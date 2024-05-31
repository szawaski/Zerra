// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterGuid<TParent> : ByteConverter<TParent, Guid>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out Guid value)
            => reader.TryReadGuid(out value, out state.BytesNeeded);

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, Guid value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}