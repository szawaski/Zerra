// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterArray<TParent, TValue> : ByteConverter<TParent, TValue?[]>
    {
        private Func<int, ArrayAccessor<TValue?>> creator = null!;
        private ByteConverter<ArrayAccessor<TValue?>> converter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            var parent = TypeAnalyzer<TValue?[]>.GetTypeDetail();

            var args = new object[1];
            creator = (length) => new ArrayAccessor<TValue?>(length);

            Func<ArrayAccessor<TValue?>, TValue?> getter = (parent) => parent.Get();
            Action<ArrayAccessor<TValue?>, TValue?> setter = (parent, value) => parent.Set(value);

            var converterRoot = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, getter, setter);
            converter = ByteConverterFactory<ArrayAccessor<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, converterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue?[]? value)
        {
            ArrayAccessor<TValue?> accessor;

            int length;
            int sizeNeeded;
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
                    accessor = creator(length);
                    value = accessor.Array;
                    state.CurrentFrame.ResultObject = value;
                }
                else
                {
                    value = default;
                    accessor = new ArrayAccessor<TValue?>(value, 0);
                }

                if (length == 0)
                {
                    value = (TValue?[]?)state.CurrentFrame.ResultObject;
                    return true;
                }
            }
            else
            {
                value = (TValue?[]?)state.CurrentFrame.ResultObject;
                accessor = new ArrayAccessor<TValue?>(value, state.CurrentFrame.EnumerablePosition);
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                state.PushFrame(converter, valueIsNullable);
                converter.Read(ref reader, ref state, accessor);
                if (state.BytesNeeded > 0)
                    return false;

                state.CurrentFrame.EnumerablePosition = accessor.Index;

                if (state.CurrentFrame.EnumerablePosition == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue?[]? value)
        {
            if (value == null)
                throw new NotSupportedException();

            ArrayAccessor<TValue?> accessor;

            int sizeNeeded;
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
                while (accessor.Index < accessor.Array.Length)
                {
                    state.PushFrame(converter, valueIsNullable, accessor);
                    converter.Write(ref writer, ref state, accessor);
                    if (state.BytesNeeded > 0)
                        return false;
                }
            }

            return true;
        }

        private sealed class ArrayAccessor<T>
        {
            private int index;
            public int Index => index;

            private readonly T[]? array;
            public T[]? Array => array;

            public ArrayAccessor(T[]? array, int index)
            {
                this.array = array;
                this.index = index;
            }
            public ArrayAccessor(int length)
            {
                this.array = new T[length];
                this.index = 0;
            }

            public void Set(T value)
            {
                if (array == null)
                    return;
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