// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterDictionaryT<TKey, TValue> : ByteConverter<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private ByteConverter<KeyValuePair<TKey, TValue>> converter = null!;

        protected override sealed void Setup()
        {
            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            converter = (ByteConverter<KeyValuePair<TKey, TValue>>)ByteConverterFactory.Get(keyValuePairTypeDetail, nameof(ByteConverterDictionaryT<TKey, TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Dictionary<TKey, TValue>? value)
        {
            if (state.Current.DrainBytes)
            {
                value = default;
                if (!state.Current.EnumerableLength.HasValue)
                {
                    if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                        return false;
                }

                var length = state.Current.EnumerableLength!.Value;
                for (; state.Current.EnumeratorIndex < length; state.Current.EnumeratorIndex++)
                {
                    if (!converter.TryReadToValue(ref reader, ref state, out _))
                        return false;
                }
                return true;
            }

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                value = new Dictionary<TKey, TValue>();
                if (state.Current.EnumerableLength!.Value == 0)
                    return true;
            }
            else
            {
                value = (Dictionary<TKey, TValue>)state.Current.Object!;
            }

            if (value.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.Object = value;
                    return false;
                }
                value.Add(item.Key, item.Value);

                if (value.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in Dictionary<TKey, TValue> value)
        {
            Dictionary<TKey, TValue>.Enumerator enumerator;

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
                enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.Object!;
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