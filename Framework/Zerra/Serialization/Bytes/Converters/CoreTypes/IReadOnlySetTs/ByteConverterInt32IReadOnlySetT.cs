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
    internal sealed class ByteConverterInt32IReadOnlySet<TParent> : ByteConverter<TParent, IReadOnlySet<int>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlySet<int>? value)
        {
            if (state.Current.NullFlags && !state.Current.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }
            }

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(false, out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }
            }

            if (!reader.TryRead(state.Current.EnumerableLength!.Value, out HashSet<int>? valueTyped, out state.BytesNeeded))
            {
                state.Current.HasNullChecked = true;
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, IReadOnlySet<int>? value)
        {
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value is null)
                {
                    if (!writer.TryWriteNull(out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out state.BytesNeeded))
                {
                    return false;
                }
            }

            if (value is null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state.");

            if (!state.Current.HasWrittenLength)
            {
                if (!writer.TryWrite(value.Count, out state.BytesNeeded))
                {
                    state.Current.HasWrittenIsNull = true;
                    return false;
                }
            }

            if (!writer.TryWrite(value, value.Count, out state.BytesNeeded))
            {
                state.Current.HasWrittenIsNull = true;
                state.Current.HasWrittenLength = true;
                return false;
            }

            return true;
        }
    }
}

#endif