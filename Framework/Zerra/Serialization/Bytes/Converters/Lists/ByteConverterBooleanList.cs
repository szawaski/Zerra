﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterBooleanList<TParent> : ByteConverter<TParent, List<bool>>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<bool>? value)
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

            int length;
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }
            }
            else
            {
                length = state.Current.EnumerableLength.Value;
            }

            if (!reader.TryReadBooleanList(length, out value, out state.BytesNeeded))
            {
                state.Current.HasNullChecked = true;
                state.Current.EnumerableLength = length;
                return false;
            }

            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, List<bool>? value)
        {
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value == null)
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

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state.");

            int length;
            if (!state.Current.EnumerableLength.HasValue)
            {
                length = value.Count;

                if (!writer.TryWrite(length, out state.BytesNeeded))
                {
                    state.Current.HasWrittenIsNull = true;
                    return false;
                }
            }
            else
            {
                length = state.Current.EnumerableLength.Value;
            }

            if (!writer.TryWrite(value, length, out state.BytesNeeded))
            {
                state.Current.HasWrittenIsNull = true;
                state.Current.EnumerableLength = length;
                return false;
            }

            return true;
        }
    }
}