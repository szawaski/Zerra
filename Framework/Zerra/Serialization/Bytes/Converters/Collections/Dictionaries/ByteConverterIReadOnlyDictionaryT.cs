// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIReadOnlyDictionaryT<TParent, TKey, TValue> : ByteConverter<TParent, IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        private ByteConverter<IDictionary<TKey, TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeConverter = null!;

        private static KeyValuePair<TKey, TValue> Getter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current;
        private static void Setter(IDictionary<TKey, TValue> parent, KeyValuePair<TKey, TValue> value) => parent.Add(value.Key, value.Value);

        protected override sealed void Setup()
        {
            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            readConverter = ByteConverterFactory<IDictionary<TKey, TValue>>.Get(keyValuePairTypeDetail, null, null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(keyValuePairTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyDictionary<TKey, TValue>? value)
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

            IDictionary<TKey, TValue> dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(false, out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    dictionary = new Dictionary<TKey, TValue>();
                    value = (IReadOnlyDictionary<TKey, TValue>?)dictionary;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    dictionary = new IDictionaryTCounter<TKey, TValue>();
                }
            }
            else
            {
                dictionary = (IDictionary<TKey, TValue>)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (IReadOnlyDictionary<TKey, TValue>?)state.Current.Object;
                else
                    value = default;
            }

            if (dictionary.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                var read = readConverter.TryReadFromParent(ref reader, ref state, dictionary, true);
                if (!read)
                {
                    state.Current.HasNullChecked = true;
                    state.Current.Object = dictionary;
                    return false;
                }

                if (dictionary.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, IReadOnlyDictionary<TKey, TValue>? value)
        {
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value is null)
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

            if (value is null) throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            if (state.Current.Object is null)
            {
                if (!writer.TryWrite(value.Count, out state.BytesNeeded))
                {
                    state.Current.HasWrittenIsNull = true;
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
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                var write = writeConverter.TryWriteFromParent(ref writer, ref state, enumerator, true);
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
    }
}