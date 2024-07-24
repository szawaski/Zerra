// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionaryOfT<TParent, TDictionary> : ByteConverter<TParent, TDictionary>
    {
        private ByteConverter<IDictionary> readConverter = null!;
        private ByteConverter<IDictionaryEnumerator> writeConverter = null!;

        private static DictionaryEntry Getter(IDictionaryEnumerator parent) => parent.Entry;
        private static void Setter(IDictionary parent, DictionaryEntry value) => parent.Add(value.Key, value.Value);

        protected override sealed void Setup()
        {
            var dictionaryEntryTypeDetail = TypeAnalyzer<DictionaryEntry>.GetTypeDetail();
            readConverter = ByteConverterFactory<IDictionary>.Get(dictionaryEntryTypeDetail, null, null, Setter);
            writeConverter = ByteConverterFactory<IDictionaryEnumerator>.Get(dictionaryEntryTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            IDictionary dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    value = typeDetail.Creator();
                    dictionary = (IDictionary)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = typeDetail.Creator();
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
                if (!readConverter.TryReadFromParent(ref reader, ref state, dictionary, true))
                {
                    state.Current.Object = value;
                    return false;
                }

                if (dictionary.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TDictionary value)
        {
            IDictionaryEnumerator enumerator;

            if (state.Current.Object is null)
            {
                var dictionary = (IDictionary)value!;
                if (!writer.TryWrite(dictionary.Count, out state.BytesNeeded))
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