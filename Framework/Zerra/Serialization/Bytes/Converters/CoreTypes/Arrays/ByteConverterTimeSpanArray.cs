﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterTimeSpanArray<TParent> : ByteConverter<TParent, TimeSpan[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeSpan[]? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TimeSpan[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}