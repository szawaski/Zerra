// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterDictionaryT<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private JsonConverter keyConverter = null!;
        private JsonConverter valueConverter = null!;

        private JsonConverter converter = null!;

        private static TKey KeyGetter(object parent) => ((IEnumerator<KeyValuePair<TKey, TValue>>)parent).Current.Key;
        private static TValue ValueGetter(object parent) => ((IEnumerator<KeyValuePair<TKey, TValue>>)parent).Current.Value;
        private static void KeySetter(object parent, TKey value) => ((DictionaryAccessor<TKey, TValue>)parent).SetKey(value);
        private static void ValueSetter(object parent, TValue value) => ((DictionaryAccessor<TKey, TValue>)parent).Add(value);

        private static KeyValuePair<TKey, TValue> Getter(object parent) => ((IEnumerator<KeyValuePair<TKey, TValue>>)parent).Current;
        private static void Setter(object parent, KeyValuePair<TKey, TValue> value) => ((Dictionary<TKey, TValue>)parent).Add(value.Key, value.Value);

        private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            var keyDetail = TypeAnalyzer<TKey>.GetTypeDetail();
            var valueDetail = TypeAnalyzer<TValue>.GetTypeDetail();

            canWriteAsProperties = keyDetail.CoreType.HasValue;

            if (canWriteAsProperties)
            {
                var thisName = this.GetType().FullName;
                keyConverter = JsonConverterFactory.Get(keyDetail, $"{thisName}_Key", KeyGetter, KeySetter);
                valueConverter = JsonConverterFactory.Get(valueDetail, $"{thisName}_Value", ValueGetter, ValueSetter);
            }
            else
            {
                var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
                converter = JsonConverterFactory.Get(keyValuePairTypeDetail, nameof(JsonConverterDictionaryT<TKey, TValue>), Getter, Setter);
            }
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

                    accessor = new DictionaryAccessor<TKey, TValue>(new Dictionary<TKey, TValue>());

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
                        if (!keyConverter.TryReadFromParent(ref reader, ref state, accessor))
                        {
                            state.Current.Object = accessor;
                            value = default;
                            return false;
                        }
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
                        if (!valueConverter.TryReadFromParentMember(ref reader, ref state, accessor, state.IncludeReturnGraph ? accessor.CurrentKeyString : null, true))
                        {
                            state.Current.HasReadFirstToken = true;
                            state.Current.HasReadProperty = true;
                            state.Current.HasReadSeperator = true;
                            state.Current.Object = accessor;
                            value = default;
                            return false;
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
                        if (!converter.TryReadFromParent(ref reader, ref state, value))
                        {
                            state.Current.HasCreated = true;
                            state.Current.HasReadFirstToken = true;
                            state.Current.Object = value;
                            return false;
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
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in Dictionary<TKey, TValue> value)
        {
            if (canWriteAsProperties)
            {
                IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
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
                    enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
                }

                while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
                {
                    var name = enumerator.Current.Key.ToString();
                    var nameSegmentBytes = writer.UseBytes ? StringHelper.EscapeAndEncodeString(name, true) : null;
                    var nameSegmentChars = writer.UseBytes ? null : StringHelper.EscapeString(name, true);
                    if (!valueConverter.TryWriteFromParentMember(ref writer, ref state, enumerator, name, nameSegmentChars, nameSegmentBytes, default, true))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Object = enumerator;
                        state.Current.EnumeratorInProgress = true;
                        return false;
                    }

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

                    if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Object = enumerator;
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