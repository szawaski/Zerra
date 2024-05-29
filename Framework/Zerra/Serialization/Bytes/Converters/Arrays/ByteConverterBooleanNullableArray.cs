// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterBooleanNullableArrayHashSet<TParent> : ByteConverter<TParent, bool?[]>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out bool?[]? value)
        {
            int sizeNeeded;
            if (state.Current.NullFlags && !state.Current.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
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
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }
            }
            else
            {
                length = state.Current.EnumerableLength.Value;
            }

            if (!reader.TryReadBooleanNullableArray(length, out value, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                state.Current.HasNullChecked = true;
                state.Current.EnumerableLength = length;
                return false;
            }

            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, bool?[]? value)
        {
            int sizeNeeded;
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            int length;
            if (!state.Current.EnumerableLength.HasValue)
            {
                length = value.Length;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    state.Current.HasWrittenIsNull = true;
                    return false;
                }
            }
            else
            {
                length = state.Current.EnumerableLength.Value;
            }

            if (!writer.TryWrite(value, length, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                state.Current.HasWrittenIsNull = true;
                state.Current.EnumerableLength = length;
                return false;
            }

            return true;
        }
    }
}