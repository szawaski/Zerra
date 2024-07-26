// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Sets
{
    internal sealed class ByteConverterIReadOnlySetT<TParent, TValue> : ByteConverter<TParent, IReadOnlySet<TValue>>
    {
        private ByteConverter<ISet<TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ISet<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = ByteConverterFactory<ISet<TValue>>.Get(valueTypeDetail, nameof(ByteConverterIReadOnlySetT<TParent, TValue>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, nameof(ByteConverterIReadOnlySetT<TParent, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlySet<TValue>? value)
        {
            ISet<TValue> set;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
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
                if (!readConverter.TryReadFromParent(ref reader, ref state, set, true, false))
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
                if (!writer.TryWrite(value.Count, out state.BytesNeeded))
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
                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator, true))
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