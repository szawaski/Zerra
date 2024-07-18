// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Collections
{
    internal sealed class JsonConverterIReadOnlyCollectionT<TParent, TValue> : JsonConverter<TParent, IReadOnlyCollection<TValue>>
    {
        private JsonConverter<ArrayOrListAccessor<TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ArrayOrListAccessor<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<ArrayOrListAccessor<TValue>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out IReadOnlyCollection<TValue>? value)
        {
            if (valueType == JsonValueType.Null_Completed)
            {
                value = default;
                return true;
            }

            if (valueType != JsonValueType.Array)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            ArrayOrListAccessor<TValue> accessor;
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = Array.Empty<TValue>();
                    return true;
                }

                reader.BackOne();

                if (reader.TryPeakArrayLength(out var length))
                    accessor = new ArrayOrListAccessor<TValue>(new TValue[length]);
                else
                    accessor = new ArrayOrListAccessor<TValue>();
            }
            else
            {
                accessor = (ArrayOrListAccessor<TValue>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!readConverter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.HasCreated = true;
                        state.Current.Object = accessor;
                        value = default;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    state.Current.Object = accessor;
                    value = default;
                    return false;
                }

                if (c == ']')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }

            value = accessor.ToArray();
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, IReadOnlyCollection<TValue> value)
        {
            IEnumerator<TValue> enumerator;

            if (!state.Current.HasWrittenStart)
            {
                if (value.Count == 0)
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
                    if (!writer.TryWriteComma(out state.CharsNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.Object = enumerator;
                        return false;
                    }
                }

                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator))
                {
                    state.Current.HasWrittenStart = true;
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

            if (!writer.TryWriteCloseBracket(out state.CharsNeeded))
            {
                state.Current.HasWrittenStart = true;
                state.Current.Object = enumerator;
                return false;
            }
            return true;
        }
    }
}