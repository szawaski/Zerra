// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

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
            }

            ArrayAccessor<TValue?> accessor;

            int length;
            if (state.Current.Object is null)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    accessor = new ArrayAccessor<TValue?>(new TValue?[length]);
                    value = (TArray?)(object?)accessor.Array;
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
                value = (TArray?)(object?)accessor.Array;
                length = accessor.Length;
            }

            if (accessor.Index == length)
                return true;

            for (; ; )
            {
                state.PushFrame(true);
                var read = readConverter.TryReadFromParent(ref reader, ref state, accessor);
                if (!read)
                {
                    state.Current.HasNullChecked = true;
                    state.Current.Object = accessor;
                    return false;
                }
                accessor.Index++;
                if (accessor.Index == length)
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
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<TValue?> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (IReadOnlyCollection<TValue?>)value;

                var length = collection.Count;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    state.Current.HasWrittenIsNull = true;
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

                state.PushFrame(true);
                var write = writeConverter.TryWriteFromParent(ref writer, ref state, enumerator);
                if (!write)
                {
                    state.Current.HasWrittenIsNull = true;
                    state.Current.Object = enumerator;
                    return false;
                }

                state.Current.EnumeratorInProgress = false;
            }

            return true;
        }

        private sealed class ArrayAccessor<T>
        {
            public int Index;
            public readonly int Length;

            private readonly T?[]? array;
            public T?[]? Array => array;

            public ArrayAccessor(int length)
            {
                this.array = null;
                this.Index = 0;
                this.Length = length;
            }
            public ArrayAccessor(T?[] array)
            {
                this.array = array;
                this.Index = 0;
                this.Length = array.Length;
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