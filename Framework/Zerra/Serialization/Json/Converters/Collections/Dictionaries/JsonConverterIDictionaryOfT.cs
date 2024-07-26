// Copyright © KaKush LLC
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
    internal sealed class JsonConverterIDictionaryOfT<TParent, TDictionary> : JsonConverter<TParent, TDictionary>
    {
        private JsonConverter<DictionaryAccessor<object, object>> readKeyConverter = null!;
        private JsonConverter<DictionaryAccessor<object, object>> readValueConverter = null!;
        //private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeKeyConverter = null!;
        private JsonConverter<IDictionaryEnumerator> writeValueConverter = null!;

        private JsonConverter<IDictionary> readConverter = null!;
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
                readKeyConverter = JsonConverterFactory<DictionaryAccessor<object, object>>.Get(keyDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), null, KeySetter);
                readValueConverter = JsonConverterFactory<DictionaryAccessor<object, object>>.Get(valueDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), null, ValueSetter);
                //writeKeyConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), KeyGetter, null);
                writeValueConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), ValueGetter, null);
            }
            else
            {
                readConverter = JsonConverterFactory<IDictionary>.Get(keyDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), null, Setter);
                writeConverter = JsonConverterFactory<IDictionaryEnumerator>.Get(valueDetail, nameof(JsonConverterIDictionaryOfT<TParent, TDictionary>), Getter, null);
            }
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TDictionary? value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
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

                value = (TDictionary)(object)accessor.Dictionary;
                return true;
            }
            else if (valueType == JsonValueType.Array)
            {
                IDictionary dictionary;

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
                    dictionary = (IDictionary)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadValue)
                    {
                        if (!readConverter.TryReadFromParent(ref reader, ref state, dictionary))
                        {
                            state.Current.HasCreated = true;
                            state.Current.Object = dictionary;
                            value = default;
                            return false;
                        }
                    }

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        state.Current.HasCreated = true;
                        state.Current.Object = dictionary;
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

                value = (TDictionary)dictionary;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TDictionary value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
            if (canWriteAsProperties)
            {
                IDictionaryEnumerator enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    var dictionary = (IDictionary)value!;
                    if (dictionary.Count == 0)
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
                    enumerator = dictionary.GetEnumerator();
                }
                else
                {
                    enumerator = (IDictionaryEnumerator)state.Current.Enumerator!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    if (!writeValueConverter.TryWriteFromParent(ref writer, ref state, enumerator, enumerator.Entry.Key.ToString(), true))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Enumerator = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        return false;
                    }

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
                    var dictionary = (IDictionary)value!;
                    if (dictionary.Count == 0)
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
                    enumerator = dictionary.GetEnumerator();
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
                            state.Current.Enumerator = enumerator;
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