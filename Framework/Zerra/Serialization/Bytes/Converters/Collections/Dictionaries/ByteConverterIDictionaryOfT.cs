// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionaryOfT<TDictionary> : ByteConverter<TDictionary>
    {
        private ByteConverter<DictionaryEntry> converter = null!;

        protected override sealed void Setup()
        {
            var dictionaryEntryTypeDetail = TypeAnalyzer<DictionaryEntry>.GetTypeDetail();
            converter = (ByteConverter<DictionaryEntry>)ByteConverterFactory.Get(dictionaryEntryTypeDetail, nameof(ByteConverterIDictionaryOfT<TDictionary>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            IDictionary dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    value = TypeDetail.Creator!();
                    dictionary = (IDictionary)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    value = TypeDetail.Creator!();
                    dictionary = (IDictionary)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
            }
            else
            {
                dictionary = (IDictionary)state.Current.Object!;
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
                    state.Current.Object = value;
                    return false;
                }
                dictionary.Add(item.Key, item.Value);

                if (dictionary.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TDictionary value)
        {
            IDictionaryEnumerator enumerator;

            if (state.Current.Object is null)
            {
                var dictionary = (IDictionary)value!;
                if (!writer.TryWrite(dictionary.Count, out state.SizeNeeded))
                {
                    return false;
                }
                if (dictionary.Count == 0)
                {
                    return true;
                }

                enumerator = dictionary.GetEnumerator();
            }
            else
            {
                enumerator = (IDictionaryEnumerator)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Entry))
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