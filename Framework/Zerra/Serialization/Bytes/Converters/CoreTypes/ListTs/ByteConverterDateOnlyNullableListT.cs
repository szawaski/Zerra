﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterDateOnlyNullableList<TParent> : ByteConverter<TParent, List<DateOnly?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<DateOnly?>? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<DateOnly?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}

#endif