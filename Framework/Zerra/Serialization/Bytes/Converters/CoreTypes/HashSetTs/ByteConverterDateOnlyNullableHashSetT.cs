// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs
{
    internal sealed class ByteConverterDateOnlyNullableHashSet : ByteConverter<HashSet<DateOnly?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out HashSet<DateOnly?>? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in HashSet<DateOnly?> value)
            => writer.TryWrite(value, value.Count, out state.SizeNeeded);
    }
}

#endif