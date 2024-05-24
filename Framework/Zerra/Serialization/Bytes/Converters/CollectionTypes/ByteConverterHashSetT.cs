// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterHashSetT<TParent, TSet, TValue> : ByteConverter<TParent, TSet>
    {
        private ByteConverter<ISet<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            Action<ISet<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);
            var readConverterRoot = ByteConverterFactory<ISet<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, null, setter);
            readConverter = ByteConverterFactory<ISet<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            Func<IEnumerator<TValue?>, TValue?> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TSet? value)
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

            ISet<TValue?> set;

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
#if NETSTANDARD2_0
                    set = new HashSet<TValue?>();
#else
                    set = new HashSet<TValue?>(length);
#endif
                    value = (TSet?)set;
                    state.CurrentFrame.ResultObject = set;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    set = new SetCounter();
                    state.CurrentFrame.ResultObject = set;
                }
            }
            else
            {
                set = (HashSet<TValue?>?)state.CurrentFrame.ResultObject!;
                if (!state.CurrentFrame.DrainBytes)
                    value = (TSet?)state.CurrentFrame.ResultObject;
                else
                    value = default;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (set.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable, value);
                var read = readConverter.Read(ref reader, ref state, set);
                if (!read)
                    return false;

                if (set.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TSet? value)
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

        private sealed class SetCounter : ISet<TValue?>
        {
            private int count;
            public int Count => count;

            public void Add(TValue? item)
            {
                count++;
            }

            public bool IsReadOnly => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(TValue? item) => throw new NotImplementedException();
            public void CopyTo(TValue?[] array, int arrayIndex) => throw new NotImplementedException();
            public void ExceptWith(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public IEnumerator<TValue> GetEnumerator() => throw new NotImplementedException();
            public void IntersectWith(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool IsProperSubsetOf(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool IsProperSupersetOf(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool IsSubsetOf(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool IsSupersetOf(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool Overlaps(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public bool Remove(TValue? item) => throw new NotImplementedException();
            public bool SetEquals(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public void SymmetricExceptWith(IEnumerable<TValue?> other) => throw new NotImplementedException();
            public void UnionWith(IEnumerable<TValue?> other) => throw new NotImplementedException();
            bool ISet<TValue?>.Add(TValue? item) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}