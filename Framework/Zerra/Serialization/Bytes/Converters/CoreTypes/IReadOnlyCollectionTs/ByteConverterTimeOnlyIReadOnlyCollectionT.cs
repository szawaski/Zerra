﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyCollectionTs
{
    internal sealed class ByteConverterTimeOnlyIReadOnlyCollection<TParent> : ByteConverter<TParent, IReadOnlyCollection<TimeOnly>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyCollection<TimeOnly>? value)
        {
            if (!reader.TryRead(out List<TimeOnly>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyCollection<TimeOnly> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}

#endif