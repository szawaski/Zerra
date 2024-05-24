// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterEnumerable<TParent, TValue> : ByteConverter<TParent, IEnumerable<TValue?>>
    {
        private ByteConverter<ArrayAccessor<TValue?>> converter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            Func<ArrayAccessor<TValue?>, TValue?> getter = (parent) => parent.Get();
            Action<ArrayAccessor<TValue?>, TValue?> setter = (parent, value) => parent.Set(value);

            var converterRoot = ByteConverterFactory<ArrayAccessor<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, getter, setter);
            converter = ByteConverterFactory<ArrayAccessor<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, converterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out IEnumerable<TValue?>? value)
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
                    accessor = new ArrayAccessor<TValue?>(length);
                    value = accessor.Array;
                }
                else
                {
                    value = default;
                    accessor = new ArrayAccessor<TValue?>(0);
                }

                state.CurrentFrame.ResultObject = accessor;

                if (length == 0)
                {
                    return true;
                }
            }
            else
            {
                accessor = (ArrayAccessor<TValue?>)state.CurrentFrame.ResultObject!;
                value = accessor.Array;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (accessor.Index == length)
                return true;

            for (; ; )
            {
                state.PushFrame(converter, valueIsNullable, accessor);
                var read = converter.Read(ref reader, ref state, accessor);
                if (!read)
                    return false;

                if (accessor.Index == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, IEnumerable<TValue?>? value)
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
                int length;
                if (typeDetail.IsICollection)
                {
                    var collection = (ICollection)value;
                    length = collection.Count;
                }
                else if (typeDetail.IsICollectionGeneric)
                {
                    length = (int)typeDetail.GetMember("Count").GetterBoxed(value)!;
                }
                else
                {
                    var enumerable = (IEnumerable)value;
                    length = 0;
                    foreach (var item in enumerable)
                        length++;
                }

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }

                state.CurrentFrame.EnumerableLength = length;

                accessor = new ArrayAccessor<TValue?>(length);
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
            public int Index => index;

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