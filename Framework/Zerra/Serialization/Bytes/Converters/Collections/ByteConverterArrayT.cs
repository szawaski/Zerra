// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterArrayT<TParent, TArray, TValue> : ByteConverter<TParent, TArray>
    {
        private ByteConverter<ArrayAccessor<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private static TValue? Getter(IEnumerator<TValue?> parent) => parent.Current;
        private static void Setter(ArrayAccessor<TValue?> parent, TValue? value) => parent.Set(value);

        protected override void Setup()
        {
            this.readConverter = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, null, Setter);
            this.writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, null);
        }

        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TArray? value)
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
                    value = (TArray?)(object?)accessor.Array;
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
                value = (TArray?)(object?)accessor.Array;
            }

            length = state.Current.EnumerableLength.Value;
            if (accessor.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, true, accessor);
                var read = readConverter.TryReadFromParent(ref reader, ref state, accessor);
                if (!read)
                {
                    state.Current.Object = accessor;
                    return false;
                }

                if (accessor.Count == length)
                    return true;
            }
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TArray? value)
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

            IEnumerator<TValue?> enumerator;

            if (!state.Current.EnumerableLength.HasValue)
            {
                var collection = (IReadOnlyCollection<TValue?>)value;

                var length = collection.Count;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                if (length == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue?>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                state.Current.EnumeratorInProgress = true;

                state.PushFrame(writeConverter, true, value);
                var write = writeConverter.TryWriteFromParent(ref writer, ref state, enumerator);
                if (!write)
                {
                    state.Current.Object = enumerator;
                    return false;
                }

                state.Current.EnumeratorInProgress = false;
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
        }
    }
}