﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterBooleanNullableArray<TParent> : ByteConverter<TParent, bool?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out bool?[]? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in bool?[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}