﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ICollectionTs
{
    internal sealed class ByteConverterTimeOnlyNullableICollection<TParent> : ByteConverter<TParent, ICollection<TimeOnly?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ICollection<TimeOnly?>? value)
        {
            if (!reader.TryRead(out List<TimeOnly?>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ICollection<TimeOnly?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}

#endif