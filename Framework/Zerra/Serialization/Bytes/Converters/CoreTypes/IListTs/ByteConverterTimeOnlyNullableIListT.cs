﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.ILists
{
    internal sealed class ByteConverterTimeOnlyNullableIList<TParent> : ByteConverter<TParent, IList<TimeOnly?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IList<TimeOnly?>? value)
        {
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }
            }

            if (!reader.TryRead(state.Current.EnumerableLength!.Value, out List<TimeOnly?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, IList<TimeOnly?> value)
        {
            if (!state.Current.HasWrittenLength)
            {
                if (!writer.TryWrite(value.Count, out state.BytesNeeded))
                {
                    return false;
                }
            }

            if (!writer.TryWrite(value, value.Count, out state.BytesNeeded))
            {
                state.Current.HasWrittenLength = true;
                return false;
            }

            return true;
        }
    }
}

#endif