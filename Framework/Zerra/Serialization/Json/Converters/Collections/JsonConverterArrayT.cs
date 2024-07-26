// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class JsonConverterArrayT<TParent, TValue> : JsonConverter<TParent, TValue[]>
    {
        private JsonConverter<ArrayOrListAccessor<TValue>> converter = null!;

        private static TValue Getter(ArrayOrListAccessor<TValue> parent) => parent.Get();
        private static void Setter(ArrayOrListAccessor<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = JsonConverterFactory<ArrayOrListAccessor<TValue>>.Get(valueTypeDetail, nameof(JsonConverterArrayT<TParent, TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue[]? value)
        {
            if (valueType != JsonValueType.Array)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            char c;
            ArrayOrListAccessor<TValue> accessor;

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
                    if (!converter.TryReadFromParent(ref reader, ref state, accessor))
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue[] value)
        {
            ArrayOrListAccessor<TValue> accessor;

            if (!state.Current.HasWrittenStart)
            {
                if (value.Length == 0)
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
                accessor = new ArrayOrListAccessor<TValue>(value);
            }
            else
            {
                accessor = (ArrayOrListAccessor<TValue>)state.Current.Object!;
            }

            while (accessor.Index < accessor.Length)
            {
                if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWriteComma(out state.CharsNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.Object = accessor;
                        return false;
                    }
                }

                if (!converter.TryWriteFromParent(ref writer, ref state, accessor))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.HasWrittenFirst = true;
                    state.Current.HasWrittenSeperator = true;
                    state.Current.Object = accessor;
                    return false;
                }
                accessor.Index++;

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
                state.Current.HasWrittenFirst = true;
                state.Current.Object = accessor;
                return false;
            }
            return true;
        }
    }
}