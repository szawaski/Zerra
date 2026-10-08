// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Globalization;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionaryOfT<TDictionary> : JsonConverter<TDictionary>
    {
        private JsonConverter<object> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = (JsonConverter<object>)JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterIDictionaryOfT<TDictionary>), null, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TDictionary? value)
        {
            if (token != JsonToken.ObjectStart)
            {
                if (state.ErrorOnReadMismatchedData)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }

            IDictionary dictionary;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                if (!TypeDetail.HasCreator)
                    throw new InvalidOperationException($"{TypeDetail.Type} does not have a public parameterless constructor.");
                value = TypeDetail.Creator!();
                dictionary = (IDictionary)value!;

                if (reader.Token == JsonToken.ObjectEnd)
                {
                    value = (TDictionary)dictionary;
                    return true;
                }

                state.Current.HasCreated = true;
            }
            else
            {
                dictionary = (IDictionary)state.Current.Object!;
            }

            for (; ; )
            {
                string key;
                if (!state.Current.HasReadProperty)
                {
                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.Object = dictionary;
                            value = default;
                            return false;
                        }
                    }

                    if (reader.Token != JsonToken.String)
                        throw reader.CreateException();
                    if (reader.UseBytes)
                        key = reader.UnescapeStringBytes();
                    else
                        key = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                }
                else
                {
                    key = (string)state.Current.Property!;
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.Property = key;
                        state.Current.Object = dictionary;
                        value = default;
                        return false;
                    }
                    if (reader.Token != JsonToken.PropertySeperator)
                        throw reader.CreateException();
                }

                if (!state.Current.HasReadValue)
                {
                    if (state.Current.ChildJsonToken == JsonToken.NotDetermined && !reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.Property = key;
                        state.Current.Object = dictionary;
                        value = default;
                        return false;
                    }
                    if (!converter.TryReadToValue(ref reader, ref state, out var item))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.Property = key;
                        state.Current.Object = dictionary;
                        value = default;
                        return false;
                    }
                    if (state.ErrorOnReadMismatchedData)
                        dictionary.Add(key, item);
                    else
                        dictionary[key] = item;
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    state.Current.Property = key;
                    state.Current.Object = dictionary;
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

            value = (TDictionary)dictionary;
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TDictionary value)
        {
            IDictionaryEnumerator enumerator;
            if (!state.Current.HasWrittenStart)
            {
                if (((IDictionary)value!).Count == 0)
                {
                    if (!writer.TryWriteEmptyBrace(out state.SizeNeeded))
                        return false;
                    return true;
                }

                if (!writer.TryWriteOpenBrace(out state.SizeNeeded))
                    return false;
                enumerator = ((IDictionary)value!).GetEnumerator();
            }
            else
            {
                enumerator = (IDictionaryEnumerator)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                var key = enumerator.Key;
                string name;
                if (key is string keyString)
                    name = keyString;
                else if (key is DateTime keyDateTime)
                    name = keyDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
                else if (key is DateTimeOffset keyDateTimeOffset)
                    name = keyDateTimeOffset.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
#if !NETSTANDARD2_0
                else if (key is DateOnly keyDateOnly)
                    name = keyDateOnly.ToString("O", CultureInfo.InvariantCulture);
                else if (key is TimeOnly keyTimeOnly)
                    name = keyTimeOnly.ToTimeSpan().ToString("c", CultureInfo.InvariantCulture);
#endif
                else if (key is Enum keyEnum)
                    name = state.EnumAsNumber ? keyEnum.ToString("D") : EnumName.GetName(key.GetType(), key);
                else if (key is IFormattable formattable)
                    name = formattable.ToString(null, CultureInfo.InvariantCulture);
                else
                    name = key.ToString()!;

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
                if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Value, name))
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
                state.Current.Object = enumerator;
                return false;
            }
            return true;
        }
    }
}
