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
    internal sealed class ByteConverterListT<TParent, TList, TValue> : ByteConverter<TParent, TList>
    {
        private ByteConverter<IList<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            Action<IList<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);
            var readConverterRoot = ByteConverterFactory<IList<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, null, setter);
            readConverter = ByteConverterFactory<IList<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            Func<IEnumerator<TValue?>, TValue?> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TList? value)
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

            IList<TValue?> list;

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
                    list = new List<TValue?>(length);
                    value = (TList?)list;
                    state.CurrentFrame.ResultObject = list;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    list = new ListCounter();
                    state.CurrentFrame.ResultObject = list;
                }
            }
            else
            {
                list = (List<TValue?>)state.CurrentFrame.ResultObject!;
                if (!state.CurrentFrame.DrainBytes)
                    value = (TList?)state.CurrentFrame.ResultObject;
                else
                    value = default;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (list.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable, value);
                var read = readConverter.Read(ref reader, ref state, list);
                if (!read)
                    return false;

                if (list.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TList? value)
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

        private sealed class ListCounter : IList<TValue?>
        {
            private int count;
            public int Count => count;

            public void Add(TValue? item)
            {
                count++;
            }

            public TValue? this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool IsReadOnly => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(TValue? item) => throw new NotImplementedException();
            public void CopyTo(TValue?[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<TValue?> GetEnumerator() => throw new NotImplementedException();
            public int IndexOf(TValue? item) => throw new NotImplementedException();
            public void Insert(int index, TValue? item) => throw new NotImplementedException();
            public bool Remove(TValue? item) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}