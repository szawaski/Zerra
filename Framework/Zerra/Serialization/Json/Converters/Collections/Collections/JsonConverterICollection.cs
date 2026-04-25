// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Collections
{
    internal sealed class JsonConverterICollection : JsonConverter<ICollection>
    {
        private JsonConverter converter = null!;

        private static object Getter(object parent) => ((IEnumerator)parent).Current;
        private static void Setter(object parent, object value) => ((ArrayOrListAccessor<object>)parent).Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterICollection), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out ICollection? value)
        {
            if (token != JsonToken.ArrayStart)
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }

            ArrayOrListAccessor<object> accessor;

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
                    value = Array.Empty<object>();
                    return true;
                }

                if (reader.TryPeakArrayLength(out var length))
                    accessor = new ArrayOrListAccessor<object>(new object[length]);
                else
                    accessor = new ArrayOrListAccessor<object>();
            }
            else
            {
                accessor = (ArrayOrListAccessor<object>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadFirstToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasCreated = true;
                        state.Current.Object = accessor;
                        value = default;
                        return false;
                    }
                }

                if (!state.Current.HasReadValue)
                {
                    if (!converter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = accessor;
                        value = default;
                        return false;
                    }
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    state.Current.HasReadFirstToken = true;
                    state.Current.Object = accessor;
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

            value = accessor.ToArray();
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in ICollection value)
        {
            IEnumerator enumerator;

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
                enumerator = (IEnumerator)state.Current.Object!;
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

                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
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