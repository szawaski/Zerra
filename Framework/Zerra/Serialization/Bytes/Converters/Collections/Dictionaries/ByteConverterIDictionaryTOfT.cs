// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionaryTOfT<TDictionary, TKey, TValue> : ByteConverter<TDictionary>
        where TKey : notnull
    {
        private ByteConverter<KeyValuePair<TKey, TValue>> converter = null!;

        protected override sealed void Setup()
        {
            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            converter = (ByteConverter<KeyValuePair<TKey, TValue>>)ByteConverterFactory.Get(keyValuePairTypeDetail, nameof(ByteConverterIDictionaryTOfT<TDictionary, TKey, TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            IDictionary<TKey, TValue> dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (TypeDetail.Type.IsInterface || TypeDetail.Type.Name == "Dictionary`2")
                    {
                        dictionary = new Dictionary<TKey, TValue>();
                        value = (TDictionary?)dictionary;
                    }
                    else
                    {
                        if (!TypeDetail.HasCreator)
                            throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                        value = TypeDetail.Creator!();
                        dictionary = (IDictionary<TKey, TValue>)value!;
                    }
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
                    value = (TDictionary?)state.Current.Object;
                else
                    value = default;
            }

            if (dictionary.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.Object = dictionary;
                    return false;
                }
                dictionary.Add(item.Key, item.Value);

                if (dictionary.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TDictionary value)
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (ICollection<KeyValuePair<TKey, TValue>>)value!;

                if (!writer.TryWrite(collection.Count, out state.SizeNeeded))
                {
                    return false;
                }
                if (collection.Count == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Current))
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