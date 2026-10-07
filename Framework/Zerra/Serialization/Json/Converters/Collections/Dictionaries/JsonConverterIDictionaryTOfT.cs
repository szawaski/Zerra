// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionaryTOfT<TDictionary, TKey, TValue> : JsonConverter<TDictionary>
        where TKey : notnull
    {
        private JsonConverter<TKey> keyConverter = null!;
        private JsonConverter<TValue> valueConverter = null!;

        private JsonConverter<KeyValuePair<TKey, TValue>> converter = null!;

        private static void ValueSetter(object parent, TValue value) => ((IDictionaryAccessor<TKey, TValue>)parent).Add(value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<TKey>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<TValue>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue;

            if (canWriteAsProperties)
            {
                var thisName = this.GetType().FullName;
                keyConverter = (JsonConverter<TKey>)JsonConverterFactory.Get(keyDetail, $"{thisName}_Key", null, null);
                valueConverter = (JsonConverter<TValue>)JsonConverterFactory.Get(valueDetail, $"{thisName}_Value", null, ValueSetter);
            }
            else
            {
                var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
                converter = (JsonConverter<KeyValuePair<TKey, TValue>>)JsonConverterFactory.Get(keyValuePairTypeDetail, nameof(JsonConverterIDictionaryTOfT<TDictionary, TKey, TValue>), null, null);
            }
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TDictionary? value)
        {
            if (token == JsonToken.ObjectStart && canWriteAsProperties)
            {
                IDictionaryAccessor<TKey, TValue> accessor;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    accessor = new IDictionaryAccessor<TKey, TValue>((IDictionary<TKey, TValue>)TypeDetail.Creator!()!);

                    if (reader.Token == JsonToken.ObjectEnd)
                    {
                        value = (TDictionary)accessor.Dictionary;
                        return true;
                    }

                    state.Current.HasCreated = true;

                    if (state.IncludeReturnGraph)
                        state.Current.ReturnGraph = new Graph();
                }
                else
                {
                    accessor = (IDictionaryAccessor<TKey, TValue>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                    }

                    if (!state.Current.HasReadProperty)
                    {
                        if (!keyConverter.TryReadToValue(ref reader, ref state, out var key))
                        {
                            state.Current.HasReadFirstToken = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                        accessor.SetKey(key!);
                    }

                    if (!state.Current.HasReadSeperator)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.HasReadFirstToken = true;
                            state.Current.HasReadProperty = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
                        if (reader.Token != JsonToken.PropertySeperator)
                            throw reader.CreateException();
                    }

                    if (!state.Current.HasReadValue)
                    {
                        if (state.IncludeReturnGraph)
                        {
                            if (!valueConverter.TryReadFromParentMember(ref reader, ref state, accessor, accessor.CurrentKeyString, true))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
                                state.Current.Object = accessor;
                                value = default;
                                return false;
                            }
                        }
                        else
                        {
                            if (state.Current.ChildJsonToken == JsonToken.NotDetermined && !reader.TryReadToken(out state.SizeNeeded))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
                                state.Current.Object = accessor;
                                value = default;
                                return false;
                            }
                            if (!valueConverter.TryReadToValue(ref reader, ref state, out var item))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
                                state.Current.Object = accessor;
                                value = default;
                                return false;
                            }
                            accessor.Add(item!);
                        }
                    }

                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.HasReadValue = true;
                        state.Current.Object = accessor;
                        value = default;
                        return false;
                    }

                    if (reader.Token == JsonToken.ObjectEnd)
                        break;

                    if (reader.Token != JsonToken.NextItem)
                        throw reader.CreateException();

                    state.Current.HasReadFirstToken = false;
                    state.Current.HasReadProperty = false;
                    state.Current.HasReadSeperator = false;
                    state.Current.HasReadValue = false;
                }

                value = (TDictionary)accessor.Dictionary;
                return true;
            }
            else if (token == JsonToken.ArrayStart)
            {
                IDictionary<TKey, TValue> dictionary;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    dictionary = (IDictionary<TKey, TValue>)TypeDetail.Creator!()!;

                    if (reader.Token == JsonToken.ArrayEnd)
                    {
                        value = (TDictionary)dictionary;
                        return true;
                    }
                }
                else
                {
                    dictionary = (IDictionary<TKey, TValue>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.HasCreated = true;
                            state.Current.Object = dictionary;
                            value = default;
                            return false;
                        }
                    }

                    if (!state.Current.HasReadValue)
                    {
                        if (!converter.TryReadToValue(ref reader, ref state, out var pair))
                        {
                            state.Current.HasCreated = true;
                            state.Current.HasReadFirstToken = true;
                            state.Current.Object = dictionary;
                            value = default;
                            return false;
                        }
                        dictionary.Add(pair.Key, pair.Value);
                    }

                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = dictionary;
                        state.Current.HasReadValue = true;
                        value = default;
                        return false;
                    }

                    if (reader.Token == JsonToken.ArrayEnd)
                        break;

                    if (reader.Token != JsonToken.NextItem)
                        throw reader.CreateException();

                    state.Current.HasReadFirstToken = false;
                    state.Current.HasReadValue = false;
                }

                value = (TDictionary)dictionary;
                return true;
            }
            else
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
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
                        if (!writer.TryWriteEmptyBrace(out state.SizeNeeded))
                        {
                            return false;
                        }
                        return true;
                    }

                    if (!writer.TryWriteOpenBrace(out state.SizeNeeded))
                    {
                        return false;
                    }
                    enumerator = dictionary.GetEnumerator();
                }
                else
                {
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    var name = enumerator.Current.Key switch
                    {
                        string str => str,
                        //dates use the ISO 8601 formats they are read with instead of their culture formats
                        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
#if !NETSTANDARD2_0
                        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
                        TimeOnly timeOnly => timeOnly.ToTimeSpan().ToString("c", CultureInfo.InvariantCulture),
#endif
                        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                        _ => enumerator.Current.Key.ToString(),
                    };
                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (!writer.TryWritePropertyName(name, state.Current.HasWrittenFirst, out state.SizeNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            state.Current.Object = enumerator;
                            state.Current.EnumeratorInProgress = true;
                            return false;
                        }
                        if (!state.Current.HasWrittenFirst)
                            state.Current.HasWrittenFirst = true;
                    }
                    if (!valueConverter.TryWriteFromValue(ref writer, ref state, enumerator.Current.Value, name))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Object = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.HasWrittenPropertyName = true;
                        return false;
                    }

                    if (state.Current.HasWrittenPropertyName)
                        state.Current.HasWrittenPropertyName = false;
                    if (state.Current.EnumeratorInProgress)
                        state.Current.EnumeratorInProgress = false;
                }

                if (!writer.TryWriteCloseBrace(out state.SizeNeeded))
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
                        if (!writer.TryWriteEmptyBracket(out state.SizeNeeded))
                        {
                            return false;
                        }
                        return true;
                    }

                    if (!writer.TryWriteOpenBracket(out state.SizeNeeded))
                    {
                        return false;
                    }
                    enumerator = dictionary.GetEnumerator();
                }
                else
                {
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                    {
                        if (!writer.TryWriteComma(out state.SizeNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            state.Current.Object = enumerator;
                            state.Current.EnumeratorInProgress = true;
                            return false;
                        }
                    }

                    if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Current, null))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Object = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.HasWrittenSeperator = true;
                        return false;
                    }

                    if (!state.Current.HasWrittenFirst)
                        state.Current.HasWrittenFirst = true;
                    if (state.Current.HasWrittenSeperator)
                        state.Current.HasWrittenSeperator = false;
                    if (state.Current.EnumeratorInProgress)
                        state.Current.EnumeratorInProgress = false;
                }

                if (!writer.TryWriteCloseBracket(out state.SizeNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }
        }
    }
}