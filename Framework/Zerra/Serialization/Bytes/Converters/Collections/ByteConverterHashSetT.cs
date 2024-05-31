// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterHashSetT<TParent, TSet, TValue> : ByteConverter<TParent, TSet>
    {
        private ByteConverter<ISet<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private static TValue? Getter(IEnumerator<TValue?> parent) => parent.Current;
        private static void Setter(ISet<TValue?> parent, TValue? value) => parent.Add(value);

        protected override sealed void Setup()
        {
            this.readConverter = ByteConverterFactory<ISet<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, null, Setter);
            this.writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.Get(typeDetail.IEnumerableGenericInnerTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TSet? value)
        {
            if (state.Current.NullFlags && !state.Current.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }
            }

            ISet<TValue?> set;
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32Nullable(false, out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
#if NETSTANDARD2_0
                    set = new HashSet<TValue?>();
#else
                    set = new HashSet<TValue?>(state.Current.EnumerableLength!.Value);
#endif
                    value = (TSet?)set;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
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

            if (set.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                state.PushFrame(true);
                var read = readConverter.TryReadFromParent(ref reader, ref state, set);
                if (!read)
                {
                    state.Current.HasNullChecked = true;
                    state.Current.Object = set;
                    return false;
                }

                if (set.Count == state.Current.EnumerableLength.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TSet? value)
        {
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out state.BytesNeeded))
                {
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

                if (!writer.TryWrite(length, out state.BytesNeeded))
                {
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
                state.PushFrame(true);
                var write = writeConverter.TryWriteFromParent(ref writer, ref state, enumerator);
                if (!write)
                {
                    state.Current.HasWrittenIsNull = true;
                    state.Current.Object = enumerator;
                    state.Current.EnumeratorInProgress = true;
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