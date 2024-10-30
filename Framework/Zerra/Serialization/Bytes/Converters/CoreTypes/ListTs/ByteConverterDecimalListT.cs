﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterDecimalList<TParent> : ByteConverter<TParent, List<decimal>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<decimal>? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<decimal> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}