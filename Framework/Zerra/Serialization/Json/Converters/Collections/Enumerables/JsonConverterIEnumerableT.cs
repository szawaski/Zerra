// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Enumerables
{
    internal sealed class JsonConverterIEnumerableT<TValue> : JsonConverter<IEnumerable<TValue>>
    {
        private JsonConverter<TValue> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = (JsonConverter<TValue>)JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterIEnumerableT<TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out IEnumerable<TValue>? value)
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
                    if (!converter.TryReadToValue(ref reader, ref state, out var item))
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

            value = list;
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in IEnumerable<TValue> value)
        {
            IEnumerator<TValue> enumerator;

            if (!state.Current.HasWrittenStart)
            {
                //var count = 0;
                //foreach (var item in value)
                //    count++;
                //if (count == 0)
                //{
                //    if (!writer.TryWriteEmptyBracket(out state.CharsNeeded))
                //    {
                //        return false;
                //    }
                //    return true;
                //}

                if (!writer.TryWriteOpenBracket(out state.SizeNeeded))
                {
                    return false;
                }
                enumerator = value.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWriteComma(out state.SizeNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.Object = enumerator;
                        return false;
                    }
                }

                if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Current, null))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.HasWrittenSeperator = true;
                    state.Current.EnumeratorInProgress = true;
                    state.Current.Object = enumerator;
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
                state.Current.Object = enumerator;
                return false;
            }
            return true;
        }
    }
}