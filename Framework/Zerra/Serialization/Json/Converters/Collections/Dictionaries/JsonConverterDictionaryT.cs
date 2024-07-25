﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterDictionaryT<TParent, TKey, TValue> : JsonConverter<TParent, Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private JsonConverter<DictionaryAccessor<TKey, TValue>> readKeyConverter = null!;
        private JsonConverter<DictionaryAccessor<TKey, TValue>> readValueConverter = null!;
        //private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeKeyConverter = null!;
        private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeValueConverter = null!;

        private JsonConverter<Dictionary<TKey, TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeConverter = null!;

        //private static TKey KeyGetter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current.Key;
        private static TValue ValueGetter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current.Value;
        private static void KeySetter(DictionaryAccessor<TKey, TValue> parent, TKey value) => parent.SetKey(value);
        private static void ValueSetter(DictionaryAccessor<TKey, TValue> parent, TValue value) => parent.Add(value);

        private static KeyValuePair<TKey, TValue> Getter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current;
        private static void Setter(Dictionary<TKey, TValue> parent, KeyValuePair<TKey, TValue> value) => parent.Add(value.Key, value.Value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<TKey>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<TValue>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue;

            if (canWriteAsProperties)
            {
                readKeyConverter = JsonConverterFactory<DictionaryAccessor<TKey, TValue>>.Get(keyDetail, nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), null, KeySetter);
                readValueConverter = JsonConverterFactory<DictionaryAccessor<TKey, TValue>>.Get(valueDetail, nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), null, ValueSetter);
                //writeKeyConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(valueDetail,  nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), KeyGetter, null);
                writeValueConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(valueDetail, nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), ValueGetter, null);
            }
            else
            {
                readConverter = JsonConverterFactory<Dictionary<TKey, TValue>>.Get(keyDetail, nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), null, Setter);
                writeConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(valueDetail, nameof(JsonConverterDictionaryT<TParent, TKey, TValue>), Getter, null);
            }
        }

        protected override bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out Dictionary<TKey, TValue>? value)
        {
            char c;

            if (valueType == JsonValueType.Object && canWriteAsProperties)
            {
                DictionaryAccessor<TKey, TValue> accessor;
            
                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    accessor = new DictionaryAccessor<TKey, TValue>(new Dictionary<TKey, TValue>());

                    if (c == '}')
                    {
                        value = default;
                        return true;
                    }

                    reader.BackOne();

                    state.Current.HasCreated = true;
                }
                else
                {
                    accessor = (DictionaryAccessor<TKey, TValue>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadProperty)
                    {
                        if (!readKeyConverter.TryReadFromParent(ref reader, ref state, accessor))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.HasReadSeperator = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                    }

                    if (!state.Current.HasReadSeperator)
                    {
                        if (!reader.TryReadNextSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            state.Current.HasReadProperty = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                        if (c != ':')
                            throw reader.CreateException("Unexpected character");
                    }

                    if (!state.Current.HasReadValue)
                    {
                        if (!readValueConverter.TryReadFromParent(ref reader, ref state, accessor))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.HasReadSeperator = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                    }

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.HasReadValue = true;
                        state.Current.Object = accessor;
                        value = default;
                        return false;
                    }

                    if (c == '}')
                        break;

                    if (c != ',')
                        throw reader.CreateException("Unexpected character");

                    state.Current.HasReadProperty = false;
                    state.Current.HasReadSeperator = false;
                    state.Current.HasReadValue = false;
                }

                value = accessor.Dictionary;
                return true;
            }
            else if (valueType == JsonValueType.Array)
            {
                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    value = new Dictionary<TKey, TValue>();

                    if (c == ']')
                        return true;

                    reader.BackOne();
                }
                else
                {
                    value = (Dictionary<TKey, TValue>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadValue)
                    {
                        if (!readConverter.TryReadFromParent(ref reader, ref state, value))
                        {
                            state.Current.HasCreated = true;
                            return false;
                        }
                    }

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        state.Current.HasCreated = true;
                        state.Current.HasReadValue = true;
                        return false;
                    }

                    if (c == ']')
                        break;

                    if (c != ',')
                        throw reader.CreateException("Unexpected character");

                    state.Current.HasReadValue = false;
                }

                return true;
            }
            else
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }
        }

        protected override bool TryWriteValue(ref JsonWriter writer, ref WriteState state, Dictionary<TKey, TValue> value)
        {
            if (canWriteAsProperties)
            {
                IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    if (value.Count == 0)
                    {
                        if (!writer.TryWriteEmptyBrace(out state.CharsNeeded))
                        {
                            return false;
                        }
                        return true;
                    }

                    if (!writer.TryWriteOpenBrace(out state.CharsNeeded))
                    {
                        return false;
                    }
                    enumerator = value.GetEnumerator();
                }
                else
                {
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Enumerator!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                    {
                        if (!writer.TryWriteComma(out state.CharsNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            state.Current.EnumeratorInProgress = true;
                            return false;
                        }
                    }

                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (!writer.TryWritePropertyName(enumerator.Current.Key.ToString(), out state.CharsNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            state.Current.Enumerator = enumerator;
                            state.Current.EnumeratorInProgress = true;
                            state.Current.HasWrittenSeperator = true;
                            return false;
                        }
                    }

                    if (!writeValueConverter.TryWriteFromParent(ref writer, ref state, enumerator))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Enumerator = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.HasWrittenSeperator = true;
                        state.Current.HasWrittenPropertyName = true;
                        return false;
                    }

                    if (!state.Current.HasWrittenFirst)
                        state.Current.HasWrittenFirst = true;
                    if (state.Current.HasWrittenSeperator)
                        state.Current.HasWrittenSeperator = false;
                    if (state.Current.HasWrittenPropertyName)
                        state.Current.HasWrittenPropertyName = false;
                    if (state.Current.EnumeratorInProgress)
                        state.Current.EnumeratorInProgress = false;
                }

                if (!writer.TryWriteCloseBrace(out state.CharsNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }
            else
            {
                IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    if (value.Count == 0)
                    {
                        if (!writer.TryWriteEmptyBracket(out state.CharsNeeded))
                        {
                            return false;
                        }
                        return true;
                    }

                    if (!writer.TryWriteOpenBracket(out state.CharsNeeded))
                    {
                        return false;
                    }
                    enumerator = value.GetEnumerator();
                }
                else
                {
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Enumerator!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                    {
                        if (!writer.TryWriteComma(out state.CharsNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            state.Current.EnumeratorInProgress = true;
                            return false;
                        }
                    }

                    if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Enumerator = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.HasWrittenSeperator = true;
                        state.Current.HasWrittenPropertyName = true;
                        return false;
                    }

                    if (!state.Current.HasWrittenFirst)
                        state.Current.HasWrittenFirst = true;
                    if (state.Current.HasWrittenSeperator)
                        state.Current.HasWrittenSeperator = false;
                    if (state.Current.HasWrittenPropertyName)
                        state.Current.HasWrittenPropertyName = false;
                    if (state.Current.EnumeratorInProgress)
                        state.Current.EnumeratorInProgress = false;
                }

                if (!writer.TryWriteCloseBracket(out state.CharsNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }
        }
    }
}