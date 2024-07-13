// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Lists
{
    internal sealed class JsonConverterIReadOnlyListT<TParent, TValue> : JsonConverter<TParent, IReadOnlyList<TValue>>
    {
        private JsonConverter<IList<TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(IList<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<IList<TValue>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, out IReadOnlyList<TValue>? value)
        {
            if (state.Current.ValueType == JsonValueType.Null_Completed)
            {
                value = default;
                return true;
            }

            if (state.Current.ValueType != JsonValueType.Array)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state);
            }

            IList<TValue> list;
            char c;

            if (!state.Current.HasReadFirstArrayElement)
            {
                if (!state.Current.HasReadValue)
                {
                    if (!state.Current.WorkingFirstChar.HasValue)
                    {
                        if (!reader.TryReadNextSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            value = default;
                            return false;
                        }
                    }
                    else
                    {
                        c = state.Current.WorkingFirstChar.Value;
                    }

                    if (c == ']')
                    {
                        list = new List<TValue>(0);
                        value = (IReadOnlyList<TValue>)list;
                        return true;
                    }

                    if (reader.TryPeakArrayLength(c, out var length))
                    {
                        list = new List<TValue>(length);
                        value = (IReadOnlyList<TValue>)list;
                    }
                    else
                    {
                        list = new List<TValue>();
                        value = (IReadOnlyList<TValue>)list;
                    }

                    reader.BackOne();

                    if (!readConverter.TryReadFromParent(ref reader, ref state, list))
                    {
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }
                else
                {
                    list = (IList<TValue>)state.Current.Object!;
                    value = (IReadOnlyList<TValue>)state.Current.Object!;
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = new List<TValue>(0);
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
            else
            {
                list = (IList<TValue>)state.Current.Object!;
                value = (IReadOnlyList<TValue>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!state.Current.WorkingFirstChar.HasValue)
                    {
                        if (!reader.TryReadNextSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            state.Current.HasReadFirstArrayElement = true;
                            value = default;
                            return false;
                        }
                    }
                    else
                    {
                        c = state.Current.WorkingFirstChar.Value;
                    }

                    if (c == ']')
                        break;

                    reader.BackOne();

                    if (!readConverter.TryReadFromParent(ref reader, ref state, list))
                    {
                        state.Current.HasReadFirstArrayElement = true;
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadFirstArrayElement = true;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (c == ']')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }

            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, IReadOnlyList<TValue> value)
        {
            if (value.Count == 0)
            {
                if (!writer.TryWriteEmptyBracket(out state.CharsNeeded))
                {
                    return false;
                }
                return true;
            }

            IEnumerator<TValue> enumerator;

            if (!state.Current.HasWrittenStart)
            {
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