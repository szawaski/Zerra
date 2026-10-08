// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterDictionaryT<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private JsonConverter<TKey> keyConverter = null!;
        private JsonConverter<TValue> valueConverter = null!;

        private JsonConverter<KeyValuePair<TKey, TValue>> converter = null!;

        private static void ValueSetter(object parent, TValue value) => ((DictionaryAccessor<TKey, TValue>)parent).Add(value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<TKey>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<TValue>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue || keyDetail.EnumUnderlyingType.HasValue;

            if (canWriteAsProperties)
            {
                var thisName = this.GetType().FullName;
                keyConverter = (JsonConverter<TKey>)JsonConverterFactory.Get(keyDetail, $"{thisName}_Key", null, null);
                valueConverter = (JsonConverter<TValue>)JsonConverterFactory.Get(valueDetail, $"{thisName}_Value", null, ValueSetter);
            }

            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            converter = (JsonConverter<KeyValuePair<TKey, TValue>>)JsonConverterFactory.Get(keyValuePairTypeDetail, nameof(JsonConverterDictionaryT<TKey, TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out Dictionary<TKey, TValue>? value)
        {
            if (token == JsonToken.ObjectStart && canWriteAsProperties)
            {
                DictionaryAccessor<TKey, TValue> accessor;

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    accessor = new DictionaryAccessor<TKey, TValue>(new Dictionary<TKey, TValue>(), state.ErrorOnReadMismatchedData);

                    if (reader.Token == JsonToken.ObjectEnd)
                    {
                        value = accessor.Dictionary;
                        return true;
                    }

                    state.Current.HasCreated = true;

                    if (state.IncludeReturnGraph)
                        state.Current.ReturnGraph = new Graph();
                }
                else
                {
                    accessor = (DictionaryAccessor<TKey, TValue>)state.Current.Object!;
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
                            if (!valueConverter.TryReadFromParentMember(ref reader, ref state, accessor, accessor.GetCurrentKeyString(state.EnumAsNumber), true))
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

                value = accessor.Dictionary;
                return true;
            }
            else if (token == JsonToken.ArrayStart)
            {
                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    value = new Dictionary<TKey, TValue>();

                    if (reader.Token == JsonToken.ArrayEnd)
                        return true;
                }
                else
                {
                    value = (Dictionary<TKey, TValue>)state.Current.Object!;
                }

                for (; ; )
                {
                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.HasCreated = true;
                            state.Current.Object = value;
                            return false;
                        }
                    }

                    if (!state.Current.HasReadValue)
                    {
                        if (state.Current.ChildJsonToken == JsonToken.NotDetermined && reader.Token != JsonToken.ObjectStart && reader.Token != JsonToken.ArrayStart)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                        }
                        else
                        {
                            if (!converter.TryReadToValue(ref reader, ref state, out var pair))
                            {
                                state.Current.HasCreated = true;
                                state.Current.HasReadFirstToken = true;
                                state.Current.Object = value;
                                return false;
                            }
                            if (pair.Key is null)
                            {
                                if (state.ErrorOnReadMismatchedData)
                                    ThrowCannotConvert(ref reader);
                            }
                            else if (state.ErrorOnReadMismatchedData)
                            {
                                value.Add(pair.Key, pair.Value);
                            }
                            else
                            {
                                value[pair.Key] = pair.Value;
                            }
                        }
                    }

                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = value;
                        state.Current.HasReadValue = true;
                        return false;
                    }

                    if (reader.Token == JsonToken.ArrayEnd)
                        break;

                    if (reader.Token != JsonToken.NextItem)
                        throw reader.CreateException();

                    state.Current.HasReadFirstToken = false;
                    state.Current.HasReadValue = false;
                }

                return true;
            }
            else
            {
                if (state.ErrorOnReadMismatchedData)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in Dictionary<TKey, TValue> value)
        {
            if (canWriteAsProperties)
            {
                Dictionary<TKey, TValue>.Enumerator enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    if (value.Count == 0)
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
                    enumerator = value.GetEnumerator();
                }
                else
                {
                    enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.Object!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var currentKey = current.Key;
                    string name;
                    if (typeof(TKey) == typeof(string))
                        name = (string)(object)currentKey;
                    else if (typeof(TKey) == typeof(DateTime) || typeof(TKey) == typeof(DateTimeOffset))
                        name = GetDateKeyName(currentKey);
#if !NETSTANDARD2_0
                    else if (typeof(TKey) == typeof(DateOnly))
                        name = ((DateOnly)(object)currentKey).ToString("O", CultureInfo.InvariantCulture);
                    else if (typeof(TKey) == typeof(TimeOnly))
                        name = ((TimeOnly)(object)currentKey).ToTimeSpan().ToString("c", CultureInfo.InvariantCulture);
#endif
                    else if (typeof(TKey).IsEnum)
                        name = state.EnumAsNumber ? ((Enum)(object)currentKey).ToString("D") : EnumName.GetName(typeof(TKey), currentKey);
                    else if (currentKey is IFormattable formattable)
                        name = formattable.ToString(null, CultureInfo.InvariantCulture);
                    else
                        name = currentKey.ToString()!;
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
                    if (!valueConverter.TryWriteFromValue(ref writer, ref state, current.Value, name))
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
                Dictionary<TKey, TValue>.Enumerator enumerator;
                if (!state.Current.HasWrittenStart)
                {
                    if (value.Count == 0)
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
                    enumerator = value.GetEnumerator();
                }
                else
                {
                    enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.Object!;
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

        // Separate method because stackalloc blocks inlining, which slows every dictionary write when it is in TryWriteValue
        private static string GetDateKeyName(TKey key)
        {
#if NETSTANDARD2_0
            return typeof(TKey) == typeof(DateTime) ? ((DateTime)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture) : ((DateTimeOffset)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
#else
            Span<char> dateChars = stackalloc char[33];
            int dateLength;
            if (typeof(TKey) == typeof(DateTime))
                _ = ((DateTime)(object)key).TryFormat(dateChars, out dateLength, "O", CultureInfo.InvariantCulture);
            else
                _ = ((DateTimeOffset)(object)key).TryFormat(dateChars, out dateLength, "O", CultureInfo.InvariantCulture);
            var fractionEnd = 27;
            while (fractionEnd > 20 && dateChars[fractionEnd - 1] == '0')
                fractionEnd--;
            if (fractionEnd == 20)
                fractionEnd = 19;
            dateChars.Slice(27, dateLength - 27).CopyTo(dateChars.Slice(fractionEnd));
            return new string(dateChars.Slice(0, fractionEnd + dateLength - 27));
#endif
        }
    }
}