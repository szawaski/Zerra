﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterBooleanNullableArrayHashSet<TParent> : ByteConverter<TParent, bool?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out bool?[]? value)
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
                if (!reader.TryReadInt32Nullable(false, out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }
            }

            if (!reader.TryReadBooleanNullableArray(state.Current.EnumerableLength!.Value, out value, out state.BytesNeeded))
            {
                state.Current.HasNullChecked = true;
                return false;
            }

            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, bool?[]? value)
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
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            int length;
            if (!state.Current.EnumerableLength.HasValue)
            {
                length = value.Length;

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