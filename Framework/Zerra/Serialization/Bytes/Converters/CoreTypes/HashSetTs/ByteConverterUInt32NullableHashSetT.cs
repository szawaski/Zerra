// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs
{
    internal sealed class ByteConverterUInt32NullableHashSet<TParent> : ByteConverter<TParent, HashSet<uint?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out HashSet<uint?>? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in HashSet<uint?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}