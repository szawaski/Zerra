// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs
{
    internal sealed class ByteConverterDecimalNullableHashSet<TParent> : ByteConverter<TParent, HashSet<decimal?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out HashSet<decimal?>? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in HashSet<decimal?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}