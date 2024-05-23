// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterArray<TParent, TValue> : ByteConverter<TParent, TValue?[]>
    {
        private Func<int, ArrayAccessor<TValue?>> creator = null!;
        private ByteConverter<ArrayAccessor<TValue?>> converter = null!;

        public override void Setup()
        {
            var parent = TypeAnalyzer<TValue?[]>.GetTypeDetail();

            var args = new object[1];
            creator = (length) => new ArrayAccessor<TValue?>(length);

            Func<ArrayAccessor<TValue?>, TValue?> getter = (parent) => parent.Get();
            Action<ArrayAccessor<TValue?>, TValue?> setter = (parent, value) => parent.Set(value);

            var converterRoot = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, getter, setter);
            converter = ByteConverterFactory<ArrayAccessor<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, converterRoot);
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue?[]? obj)
        {
            ArrayAccessor<TValue?> accessor;

            int length;
            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    obj = default;
                    return false;
                }
                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    accessor = creator(length);
                    obj = accessor.Array;
                    state.CurrentFrame.ResultObject = obj;
                }
                else
                {
                    obj = default;
                    accessor = new ArrayAccessor<TValue?>(obj, 0);
                }

                if (length == 0)
                {
                    obj = (TValue?[]?)state.CurrentFrame.ResultObject;
                    return true;
                }
            }
            else
            {
                obj = (TValue?[]?)state.CurrentFrame.ResultObject;
                accessor = new ArrayAccessor<TValue?>(obj, state.CurrentFrame.EnumerablePosition);
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                if (!state.CurrentFrame.HasNullChecked)
                {
                    if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        obj = default;
                        return false;
                    }

                    if (isNull)
                    {
                        accessor.Set(default);
                        state.CurrentFrame.EnumerablePosition = accessor.Index;
                        if (state.CurrentFrame.EnumerablePosition == length)
                        {
                            obj = default;
                            return true;
                        }
                        continue;
                    }
                    state.CurrentFrame.HasNullChecked = true;
                }

                if (!state.CurrentFrame.HasObjectStarted)
                {
                    state.PushFrame(converter, false);
                    converter.Read(ref reader, ref state, accessor);
                    if (state.BytesNeeded > 0)
                        return false;
                }

                state.CurrentFrame.HasObjectStarted = false;
                state.CurrentFrame.HasNullChecked = false;
                state.CurrentFrame.EnumerablePosition = accessor.Index;

                if (state.CurrentFrame.EnumerablePosition == length)
                {
                    return true;
                }
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue?[]? obj)
        {
            throw new NotImplementedException();
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