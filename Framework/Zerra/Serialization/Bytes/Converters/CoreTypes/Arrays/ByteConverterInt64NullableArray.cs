// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterInt64NullableArray : ByteConverter<long?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out long?[]? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in long?[] value)
            => writer.TryWrite(value, value.Length, out state.SizeNeeded);
    }
}