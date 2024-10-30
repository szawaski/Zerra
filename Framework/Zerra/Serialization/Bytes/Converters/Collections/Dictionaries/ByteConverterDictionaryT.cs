// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterDictionaryT<TParent, TKey, TValue> : ByteConverter<TParent, Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private ByteConverter<Dictionary<TKey, TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeConverter = null!;

        private static KeyValuePair<TKey, TValue> Getter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current;
        private static void Setter(Dictionary<TKey, TValue> parent, KeyValuePair<TKey, TValue> value) => parent.Add(value.Key, value.Value);

        protected override sealed void Setup()
        {
            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            readConverter = ByteConverterFactory<Dictionary<TKey, TValue>>.Get(keyValuePairTypeDetail, nameof(ByteConverterDictionaryT<TParent, TKey, TValue>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(keyValuePairTypeDetail, nameof(ByteConverterDictionaryT<TParent, TKey, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Dictionary<TKey, TValue>? value)
        {
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    value = new Dictionary<TKey, TValue>();
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = new Dictionary<TKey, TValue>();
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
            }
            else
            {
                value = (Dictionary<TKey, TValue>)state.Current.Object!;
            }

            if (value.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!readConverter.TryReadFromParent(ref reader, ref state, value, true))
                {
                    state.Current.Object = value;
                    return false;
                }

                if (value.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in Dictionary<TKey, TValue> value)
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

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
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
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