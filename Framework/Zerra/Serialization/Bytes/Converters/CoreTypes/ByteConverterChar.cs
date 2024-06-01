﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterChar<TParent> : ByteConverter<TParent, char>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out char value)
            => reader.TryReadChar(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, char value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}