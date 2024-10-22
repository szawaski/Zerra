﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ICollectionTs
{
    internal sealed class ByteConverterDateTimeOffsetICollection<TParent> : ByteConverter<TParent, ICollection<DateTimeOffset>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ICollection<DateTimeOffset>? value)
        {
            if (!reader.TryRead(out List<DateTimeOffset>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ICollection<DateTimeOffset> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}