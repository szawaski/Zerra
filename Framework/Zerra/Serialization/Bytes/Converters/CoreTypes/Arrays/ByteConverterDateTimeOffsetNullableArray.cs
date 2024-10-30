// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterDateTimeOffsetNullableArray<TParent> : ByteConverter<TParent, DateTimeOffset?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTimeOffset?[]? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in DateTimeOffset?[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}