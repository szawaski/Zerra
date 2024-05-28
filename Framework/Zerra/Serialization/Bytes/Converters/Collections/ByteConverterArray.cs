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
            this.converter = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, Setter);
        }

        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue?[]? value)
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
                    accessor = new ArrayAccessor<TValue?>(new TValue?[length]);
                    value = accessor.Array;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    accessor = new ArrayAccessor<TValue?>(length);
                }
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.Current.Object!;
                value = accessor.Array;
            }

            length = state.Current.EnumerableLength.Value;
            if (accessor.Index == length)
                return true;

            for (; ; )
            {
                state.PushFrame(true);
                var read = converter.TryReadFromParent(ref reader, ref state, accessor);
                if (!read)
                {
                    state.Current.Object = accessor;
                    return false;
                }
                accessor.Index++;
                if (accessor.Index == length)
                    return true;
            }
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue?[]? value)
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

            if (state.Current.Object == null)
            {
                var length = value.Length;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }

                accessor = new ArrayAccessor<TValue?>(value);
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.Current.Object!;
            }

            if (accessor.Array?.Length > 0)
            {
                while (accessor.Index < accessor.Array.Length)
                {
                    state.PushFrame(true);
                    var write = converter.TryWriteFromParent(ref writer, ref state, accessor);
                    if (!write)
                    {
                        state.Current.Object = accessor;
                        return false;
                    }
                    accessor.Index++;
                }
            }

            return true;
        }

        private sealed class ArrayAccessor<T>
        {
            public int Index;
            public readonly int Length;

            private readonly T?[]? array;
            public T?[]? Array => array;

            public ArrayAccessor(T?[] array)
            {
                this.array = array;
                this.Index = 0;
                this.Length = array.Length;
            }
            public ArrayAccessor(int length)
            {
                this.array = null;
                this.Index = 0;
                this.Length = length;
            }

            public T? Get()
            {
                if (array == null)
                    throw new InvalidOperationException();
                return array[Index];
            }

            public void Set(T? value)
            {
                if (array == null)
                    return;
                array[Index] = value;
            }
        }
    }
}