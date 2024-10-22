﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterInt32Array<TParent> : ByteConverter<TParent, int[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out int[]? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in int[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}