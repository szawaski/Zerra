﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterDateTime<TParent> : ByteConverter<TParent, DateTime>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTime value)
            => reader.TryReadDateTime(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTime value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}