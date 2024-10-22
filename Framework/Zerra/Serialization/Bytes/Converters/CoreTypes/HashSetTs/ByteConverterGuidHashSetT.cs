// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs
{
    internal sealed class ByteConverterGuidHashSet<TParent> : ByteConverter<TParent, HashSet<Guid>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out HashSet<Guid>? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in HashSet<Guid> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}