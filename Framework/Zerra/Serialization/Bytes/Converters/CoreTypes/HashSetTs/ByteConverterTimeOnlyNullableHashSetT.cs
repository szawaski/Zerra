// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs
{
    internal sealed class ByteConverterTimeOnlyNullableHashSet<TParent> : ByteConverter<TParent, HashSet<TimeOnly?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out HashSet<TimeOnly?>? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in HashSet<TimeOnly?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}

#endif