// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionary<TParent> : ByteConverter<TParent, IDictionary>
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

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IDictionary? value)
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
                var read = readConverter.TryReadFromParent(ref reader, ref state, value, true);
                if (!read)
                {
                    state.Current.HasNullChecked = true;
                    state.Current.Object = value;
                    return false;
                }

                if (value.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, IDictionary? value)
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

            IDictionaryEnumerator enumerator;

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
                enumerator = (IDictionaryEnumerator)state.Current.Object!;
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