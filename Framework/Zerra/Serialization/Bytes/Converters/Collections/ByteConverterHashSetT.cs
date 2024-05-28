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

        private static TValue? Getter(IEnumerator<TValue?> parent) => parent.Current;
        private static void Setter(ISet<TValue?> parent, TValue? value) => parent.Add(value);

        protected override void Setup()
        {
            this.readConverter = ByteConverterFactory<ISet<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, null, Setter);
            this.writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, null);
        }

        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TSet? value)
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

            ISet<TValue?> set;

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
#if NETSTANDARD2_0
                    set = new HashSet<TValue?>();
#else
                    set = new HashSet<TValue?>(length);
#endif
                    value = (TSet?)set;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    set = new SetCounter();
                }
            }
            else
            {
                set = (HashSet<TValue?>?)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TSet?)state.Current.Object;
                else
                    value = default;
            }

            length = state.Current.EnumerableLength.Value;
            if (set.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(true);
                var read = readConverter.TryReadFromParent(ref reader, ref state, set);
                if (!read)
                {
                    state.Current.Object = set;
                    return false;
                }

                if (set.Count == length)
                    return true;
            }
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TSet? value)
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

            if (state.Current.Object == null)
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

                state.PushFrame(true);
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