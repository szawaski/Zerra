// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Lists
{
    internal sealed class JsonConverterListT<TValue> : JsonConverter<List<TValue>>
    {
        private JsonConverter<TValue> valueConverter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            valueConverter = (JsonConverter<TValue>)JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterListT<TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out List<TValue>? value)
        {
            if (token != JsonToken.ArrayStart)
            {
                if (state.ErrorOnReadMismatchedData)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                if (reader.Token == JsonToken.ArrayEnd)
                {
                    value = new List<TValue>(0);
                    return true;
                }

                value = new List<TValue>();
            }
            else
            {
                value = (List<TValue>)state.Current.Object!;
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
                    if (!valueConverter.TryReadToValue(ref reader, ref state, out var item))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = value;
                        return false;
                    }
                    value.Add(item!);
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasCreated = true;
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadValue = true;
                    state.Current.Object = value;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in List<TValue> value)
        {
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
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < value.Count; index++)
            {
                if (index > 0 && !state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWriteComma(out state.SizeNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.EnumeratorIndex = index;
                        return false;
                    }
                }

                if (!valueConverter.TryWriteFromValue(ref writer, ref state, value[index], null))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.HasWrittenSeperator = true;
                    state.Current.EnumeratorIndex = index;
                    return false;
                }

                if (state.Current.HasWrittenSeperator)
                    state.Current.HasWrittenSeperator = false;
            }

            if (!writer.TryWriteCloseBracket(out state.SizeNeeded))
            {
                state.Current.HasWrittenStart = true;
                state.Current.EnumeratorIndex = index;
                return false;
            }
            return true;
        }
    }
}