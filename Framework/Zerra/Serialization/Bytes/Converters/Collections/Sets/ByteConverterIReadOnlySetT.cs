// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Sets
{
    internal sealed class ByteConverterIReadOnlySetT<TValue> : ByteConverter<IReadOnlySet<TValue>>
    {
        private ByteConverter converter = null!;

        private static TValue Getter(object parent) => ((IEnumerator<TValue>)parent).Current;
        private static void Setter(object parent, TValue value) => ((ISet<TValue>)parent).Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIReadOnlySetT<TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlySet<TValue>? value)
        {
            ISet<TValue> set;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    set = new HashSet<TValue>();
                    value = (IReadOnlySet<TValue>)set;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    set = new ISetTCounter<TValue>();
                }
            }
            else
            {
                set = (HashSet<TValue>?)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (IReadOnlySet<TValue>?)state.Current.Object;
                else
                    value = default;
            }

            if (set.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!converter.TryReadFromParent(ref reader, ref state, set))
                {
                    state.Current.Object = set;
                    return false;
                }

                if (set.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlySet<TValue> value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                if (!writer.TryWrite(value.Count, out state.SizeNeeded))
                {
                    return false;
                }
                if (value.Count == 0)
                {
                    return true;
                }

                enumerator = value.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
                {
                    state.Current.Object = enumerator;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }

                state.Current.EnumeratorInProgress = false;
            }

            return true;
        }
    }
}

#endif