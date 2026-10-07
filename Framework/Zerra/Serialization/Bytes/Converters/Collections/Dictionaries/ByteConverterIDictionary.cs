// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionary : ByteConverter<IDictionary>
    {
        private ByteConverter<DictionaryEntry> converter = null!;

        protected override sealed void Setup()
        {
            var dictionaryEntryTypeDetail = TypeAnalyzer<DictionaryEntry>.GetTypeDetail();
            converter = (ByteConverter<DictionaryEntry>)ByteConverterFactory.Get(dictionaryEntryTypeDetail, nameof(ByteConverterIDictionary), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IDictionary? value)
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
                    value = new Dictionary<object, object>();
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = new IDictionaryCounter();
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
            }
            else
            {
                value = (IDictionary)state.Current.Object!;
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IDictionary value)
        {
            IDictionaryEnumerator enumerator;

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