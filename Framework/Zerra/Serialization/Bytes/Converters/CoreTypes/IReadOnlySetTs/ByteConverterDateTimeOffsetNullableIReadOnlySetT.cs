﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlySetTs
{
    internal sealed class ByteConverterDateTimeOffsetNullableIReadOnlySet<TParent> : ByteConverter<TParent, IReadOnlySet<DateTimeOffset?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlySet<DateTimeOffset?>? value)
        {
            if (!reader.TryRead(out HashSet<DateTimeOffset?>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlySet<DateTimeOffset?> value)
           => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}

#endif