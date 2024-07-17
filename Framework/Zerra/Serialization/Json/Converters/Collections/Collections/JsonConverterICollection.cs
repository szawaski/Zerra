// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Collections
{
    internal sealed class JsonConverterICollection<TParent, TValue> : JsonConverter<TParent, ICollection>
    {
        private JsonConverter<ArrayAccessor<object>> readConverter = null!;
        private JsonConverter<IEnumerator> writeConverter = null!;

        private static object Getter(IEnumerator parent) => parent.Current;
        private static void Setter(ArrayAccessor<object> parent, object value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<ArrayAccessor<object>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out ICollection? value)
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

            ArrayAccessor<object> accessor;
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
                        value = Array.Empty<object>();
                        return true;
                    }

                    if (reader.TryPeakArrayLength(c, out var length))
                    {
                        accessor = new ArrayAccessor<object>(new object[length]);
                    }
                    else
                    {
                        accessor = new ArrayAccessor<object>();
                    }

                    reader.BackOne();

                    if (!readConverter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }
                else
                {
                    accessor = (ArrayAccessor<object>)state.Current.Object!;
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadValue = true;
                    state.Current.Object = accessor;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = accessor.ToArray();
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
            else
            {
                accessor = (ArrayAccessor<object>)state.Current.Object!;
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
                            state.Current.Object = accessor;
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

                    if (!readConverter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.HasReadFirstArrayElement = true;
                        state.Current.WorkingFirstChar = c;
                        state.Current.Object = accessor;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, ICollection value)
        {
            IEnumerator enumerator;

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
                enumerator = (IEnumerator)state.Current.Object!;
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