// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterArray<TParent, TValue> : ByteConverter<TParent, TValue?[]>
    {
        private ByteConverter<ArrayAccessor<TValue?>> converter = null!;

        private static TValue? Getter(ArrayAccessor<TValue?> parent) => parent.Get();
        private static void Setter(ArrayAccessor<TValue?> parent, TValue?  value) => parent.Set(value);

        protected override void Setup()
        {
            this.converter = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, Setter);
        }

        protected override bool TryRead(ref ByteReader reader, ref ReadState state, out TValue?[]? value)
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

                state.Current.HasNullChecked = true;
            }

            ArrayAccessor<TValue?> accessor;

            int length;
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }
                state.Current.EnumerableLength = length;

                if (!state.Current.DrainBytes)
                {
                    accessor = new ArrayAccessor<TValue?>(length);
                    value = accessor.Array;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    accessor = new ArrayAccessor<TValue?>();
                }
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.Current.Object!;
                value = accessor.Array;
            }

            length = state.Current.EnumerableLength.Value;
            if (accessor.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(converter, true, accessor);
                var read = converter.TryRead(ref reader, ref state, accessor);
                if (!read)
                {
                    state.Current.Object = accessor;
                    return false;
                }

                if (accessor.Count == length)
                    return true;
            }
        }

        protected override bool TryWrite(ref ByteWriter writer, ref WriteState state, TValue?[]? value)
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
                state.Current.HasWrittenIsNull = true;
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            ArrayAccessor<TValue?> accessor;

            if (!state.Current.EnumerableLength.HasValue)
            {
                var length = value.Length;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.Current.EnumerableLength = length;

                accessor = new ArrayAccessor<TValue?>(value);
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.Current.Object!;
            }

            if (accessor.Array?.Length > 0)
            {
                while (accessor.Count < accessor.Array.Length)
                {
                    state.PushFrame(converter, true, accessor);
                    var write = converter.TryWrite(ref writer, ref state, accessor);
                    if (!write)
                    {
                        state.Current.Object = accessor;
                        return false;
                    }
                }
            }

            return true;
        }

        private sealed class ArrayAccessor<T>
        {
            private int index;
            public int Count => index;

            private readonly T[]? array;
            public T[]? Array => array;

            public ArrayAccessor()
            {
                this.array = null;
                this.index = 0;
            }
            public ArrayAccessor(T[]? array)
            {
                this.array = array;
                this.index = 0;
            }
            public ArrayAccessor(int length)
            {
                this.array = new T[length];
                this.index = 0;
            }

            public void Set(T value)
            {
                if (array == null)
                {
                    index++;
                    return;
                }
                array[index++] = value;
            }

            public T Get()
            {
                if (array == null)
                    throw new InvalidOperationException();
                return array[index++];
            }
        }
    }
}