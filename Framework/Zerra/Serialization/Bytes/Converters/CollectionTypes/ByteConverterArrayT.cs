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

        private bool valueIsNullable;

        private static TValue? Getter(IEnumerator<TValue?> parent) => parent.Current;
        private static void Setter(ArrayAccessor<TValue?> parent, TValue? value) => parent.Set(value);

        public override void Setup()
        {
            var readConverterRoot = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, null, Setter);
            readConverter = ByteConverterFactory<ArrayAccessor<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TArray? value)
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

            ArrayAccessor<TValue?> accessor;

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

                if (!state.CurrentFrame.DrainBytes)
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
                    accessor = new ArrayAccessor<TValue?>(0);
                }
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.CurrentFrame.Object!;
                value = (TArray?)(object?)accessor.Array;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (accessor.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable, accessor);
                var read = readConverter.Read(ref reader, ref state, accessor);
                if (!read)
                {
                    state.CurrentFrame.Object = accessor;
                    return false;
                }

                if (accessor.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TArray? value)
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

            IEnumerator<TValue?> enumerator;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
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
                state.CurrentFrame.Object = length;

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue?>)state.CurrentFrame.Object!;
            }

            while (state.CurrentFrame.EnumeratorInProgress || enumerator.MoveNext())
            {
                state.CurrentFrame.EnumeratorInProgress = true;

                state.PushFrame(writeConverter, valueIsNullable, value);
                var write = writeConverter.Write(ref writer, ref state, enumerator);
                if (!write)
                    return false;

                state.CurrentFrame.EnumeratorInProgress = false;
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