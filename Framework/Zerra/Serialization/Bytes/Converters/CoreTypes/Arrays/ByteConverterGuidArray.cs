﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterGuidArray<TParent> : ByteConverter<TParent, Guid[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Guid[]? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in Guid[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}