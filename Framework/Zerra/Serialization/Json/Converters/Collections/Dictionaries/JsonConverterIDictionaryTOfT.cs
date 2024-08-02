// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue> : JsonConverter<TParent, TDictionary>
        where TKey : notnull
    {
        private JsonConverter<IDictionaryAccessor<TKey, TValue>> readKeyConverter = null!;
        private JsonConverter<IDictionaryAccessor<TKey, TValue>> readValueConverter = null!;
        //private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeKeyConverter = null!;
        private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeValueConverter = null!;

        private JsonConverter<IDictionary<TKey, TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeConverter = null!;

        //private static TKey KeyGetter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current.Key;
        private static TValue ValueGetter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current.Value;
        private static void KeySetter(IDictionaryAccessor<TKey, TValue> parent, TKey value) => parent.SetKey(value);
        private static void ValueSetter(IDictionaryAccessor<TKey, TValue> parent, TValue value) => parent.Add(value);

        private static KeyValuePair<TKey, TValue> Getter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current;
        private static void Setter(IDictionary<TKey, TValue> parent, KeyValuePair<TKey, TValue> value) => parent.Add(value.Key, value.Value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<TKey>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<TValue>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue;

            if (canWriteAsProperties)
            {
                readKeyConverter = JsonConverterFactory<IDictionaryAccessor<TKey, TValue>>.Get(keyDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), null, KeySetter);
                readValueConverter = JsonConverterFactory<IDictionaryAccessor<TKey, TValue>>.Get(valueDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), null, ValueSetter);
                //writeKeyConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(valueDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), KeyGetter, null);
                writeValueConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(valueDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), ValueGetter, null);
            }
            else
            {
                var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
                readConverter = JsonConverterFactory<IDictionary<TKey, TValue>>.Get(keyValuePairTypeDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), null, Setter);
                writeConverter = JsonConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(keyValuePairTypeDetail, nameof(JsonConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), Getter, null);
            }
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TDictionary? value)
        {
            char c;

            if (valueType == JsonValueType.Object && canWriteAsProperties)
            {
                IDictionaryAccessor<TKey, TValue> accessor;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    accessor = new IDictionaryAccessor<TKey, TValue>((IDictionary<TKey, TValue>)typeDetail.Creator()!);

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
                    accessor = (IDictionaryAccessor<TKey, TValue>)state.Current.Object!;
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

                value = (TDictionary)accessor.Dictionary;
                return true;
            }
            else if (valueType == JsonValueType.Array)
            {
                IDictionary<TKey, TValue> dictionary;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        value = default;
                        return false;
                    }

                    dictionary = (IDictionary<TKey, TValue>)typeDetail.Creator()!;

                    if (c == ']')
                    {
                        value = default;
                        return true;
                    }

                    reader.BackOne();
                }
                else
                {
                    dictionary = (IDictionary<TKey, TValue>)state.Current.Object!;
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
            if (canWriteAsProperties)
            {
                IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    var dictionary = (IDictionary<TKey, TValue>)value!;
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
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Enumerator!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    if (!writeValueConverter.TryWriteFromParent(ref writer, ref state, enumerator, enumerator.Current.Key.ToString(), true))
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
                IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    var dictionary = (IDictionary<TKey, TValue>)value!;
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
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Enumerator!;
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