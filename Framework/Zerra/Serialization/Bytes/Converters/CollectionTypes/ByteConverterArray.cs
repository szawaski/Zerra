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

        private bool valueIsNullable;

        private static TValue? Getter(ArrayAccessor<TValue?> parent) => parent.Get();
        private static void Setter(ArrayAccessor<TValue?> parent, TValue?  value) => parent.Set(value);

        public override void Setup()
        {
            var converterRoot = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, Setter);
            converter = ByteConverterFactory<ArrayAccessor<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, converterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue?[]? value)
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
                    value = accessor.Array;
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
                value = accessor.Array;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (accessor.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(converter, valueIsNullable, accessor);
                var read = converter.Read(ref reader, ref state, accessor);
                if (!read)
                {
                    state.CurrentFrame.Object = accessor;
                    return false;
                }

                if (accessor.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue?[]? value)
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

            ArrayAccessor<TValue?> accessor;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                var length = value.Length;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.EnumerableLength = length;

                accessor = new ArrayAccessor<TValue?>(value.Length);
                state.CurrentFrame.Object = accessor;
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.CurrentFrame.Object!;
            }

            if (accessor.Array?.Length > 0)
            {
                while (accessor.Count < accessor.Array.Length)
                {
                    state.PushFrame(converter, valueIsNullable, accessor);
                    var write = converter.Write(ref writer, ref state, accessor);
                    if (!write)
                        return false;
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