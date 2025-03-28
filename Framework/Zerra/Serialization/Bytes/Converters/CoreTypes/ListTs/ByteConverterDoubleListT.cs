﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterDoubleList<TParent> : ByteConverter<TParent, List<double>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<double>? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<double> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}