﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionary<TParent> : JsonConverter<TParent, IDictionary>
    {
        private JsonConverter<DictionaryAccessor<object, object>> readKeyConverter = null!;
        private JsonConverter<DictionaryAccessor<object, object>> readValueConverter = null!;
        //private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeKeyConverter = null!;
        private JsonConverter<IDictionaryEnumerator> writeValueConverter = null!;

        private JsonConverter<Dictionary<object, object>> readConverter = null!;
        private JsonConverter<IDictionaryEnumerator> writeConverter = null!;

        //private static TKey KeyGetter(IDictionaryEnumerator parent) => parent.Current.Key;
        private static object? ValueGetter(IDictionaryEnumerator parent) => parent.Entry.Value;
        private static void KeySetter(DictionaryAccessor<object, object> parent, object value) => parent.SetKey(value);
        private static void ValueSetter(DictionaryAccessor<object, object> parent, object value) => parent.Add(value);

        private static DictionaryEntry Getter(IDictionaryEnumerator parent) => parent.Entry;
        private static void Setter(IDictionary parent, DictionaryEntry value) => parent.Add(value.Key, value.Value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<object>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<object>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue;

            if (canWriteAsProperties)
            {
                readKeyConverter = JsonConverterFactory<DictionaryAccessor<object, object>>.Get(keyDetail, nameof(JsonConverterIDictionary<TParent>), null, KeySetter);
                readValueConverter = JsonConverterFactory<DictionaryAccessor<object, object>>.Get(valueDetail, nameof(JsonConverterIDictionary<TParent>), null, ValueSetter);
                //writeKeyConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionary<TParent>), KeyGetter, null);
                writeValueConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionary<TParent>), ValueGetter, null);
            }
            else
            {
                readConverter = JsonConverterFactory<Dictionary<object, object>>.Get(keyDetail, nameof(JsonConverterIDictionary<TParent>), null, Setter);
                writeConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionary<TParent>), Getter, null);
            }
        }

        protected override bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out IDictionary? value)
        {
            char c;

            if (valueType == JsonValueType.Object && canWriteAsProperties)
            {
                DictionaryAccessor<object, object> accessor;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    accessor = new DictionaryAccessor<object, object>(new Dictionary<object, object>());

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
                    accessor = (DictionaryAccessor<object, object>)state.Current.Object!;
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
                Dictionary<object, object> dictionary;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    dictionary = new Dictionary<object, object>();

                    if (c == ']')
                    {
                        value = default;
                        return true;
                    }

                    reader.BackOne();
                }
                else
                {
                    dictionary = (Dictionary<object, object>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadValue)
                    {
                        if (!readConverter.TryReadFromParent(ref reader, ref state, dictionary))
                        {
                            state.Current.HasCreated = true;
                            value = default;
                            return false;
                        }
                    }

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        state.Current.HasCreated = true;
                        state.Current.HasReadValue = true;
                        value = default;
                        return false;
                    }

                    if (c == ']')
                        break;

                    if (c != ',')
                        throw reader.CreateException("Unexpected character");

                    state.Current.HasReadValue = false;
                }

                value = dictionary;
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

        protected override bool TryWriteValue(ref JsonWriter writer, ref WriteState state, IDictionary value)
        {
            if (canWriteAsProperties)
            {
                IDictionaryEnumerator enumerator;
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
                    enumerator = (IDictionaryEnumerator)state.Current.Enumerator!;
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
                        if (!writer.TryWritePropertyName(enumerator.Entry.Key.ToString(), out state.CharsNeeded))
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
                IDictionaryEnumerator enumerator;
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
                    enumerator = (IDictionaryEnumerator)state.Current.Enumerator!;
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