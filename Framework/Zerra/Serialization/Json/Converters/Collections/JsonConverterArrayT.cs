// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class JsonConverterArrayT<TValue> : JsonConverter<TValue[]>
    {
        private JsonConverter<TValue> valueConverter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            valueConverter = (JsonConverter<TValue>)JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterArrayT<TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue[]? value)
        {
            if (token != JsonToken.ArrayStart)
            {
                if (state.ErrorOnReadMismatchedData)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }
            List<TValue> list;

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
                    value = Array.Empty<TValue>();
                    return true;
                }

                list = new List<TValue>();
            }
            else
            {
                list = (List<TValue>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadFirstToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasCreated = true;
                        state.Current.Object = list;
                        value = default;
                        return false;
                    }
                }

                if (!state.Current.HasReadValue)
                {
                    if (!valueConverter.TryReadToValue(ref reader, ref state, out var item))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = list;
                        value = default;
                        return false;
                    }
                    list.Add(item!);
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasCreated = true;
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadValue = true;
                    state.Current.Object = list;
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

            value = list.ToArray();
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue[] value)
        {
            if (!state.Current.HasWrittenStart)
            {
                if (value.Length == 0)
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
            for (; index < value.Length; index++)
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