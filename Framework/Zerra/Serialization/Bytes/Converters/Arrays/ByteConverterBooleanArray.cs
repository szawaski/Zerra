// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterBooleanArray<TParent> : ByteConverter<TParent, bool[]>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out bool[]? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasNullChecked)
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

                state.CurrentFrame.HasNullChecked = true;
            }

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }
                state.CurrentFrame.EnumerableLength = length;
            }
            else
            {
                length = state.CurrentFrame.EnumerableLength.Value;
            }

            if (!reader.TryReadBooleanArray(length, out value, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }

            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, bool[]? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasWrittenIsNull)
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
                state.CurrentFrame.HasWrittenIsNull = true;
            }

            if (value == null)
                throw new InvalidOperationException("Bad State");

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                length = value.Length;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.EnumerableLength = length;
            }
            else
            {
                length = state.CurrentFrame.EnumerableLength.Value;
            }

            if (!writer.TryWrite(value, length, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }

            return true;
        }
    }
}