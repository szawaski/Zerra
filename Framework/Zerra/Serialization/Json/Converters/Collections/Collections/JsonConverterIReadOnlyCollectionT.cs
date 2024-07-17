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
        private JsonConverter<ICollection<TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ICollection<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<ICollection<TValue>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, out IReadOnlyCollection<TValue>? value)
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

            ICollection<TValue> Collection;
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
                        Collection = new List<TValue>(0);
                        value = (IReadOnlyCollection<TValue>)Collection;
                        return true;
                    }

                    if (reader.TryPeakArrayLength(c, out var length))
                    {
                        Collection = new List<TValue>(length);
                        value = (IReadOnlyCollection<TValue>)Collection;
                    }
                    else
                    {
                        Collection = new List<TValue>();
                        value = (IReadOnlyCollection<TValue>)Collection;
                    }

                    reader.BackOne();

                    if (!readConverter.TryReadFromParent(ref reader, ref state, Collection))
                    {
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }
                else
                {
                    Collection = (ICollection<TValue>)state.Current.Object!;
                    value = (IReadOnlyCollection<TValue>)state.Current.Object!;
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadValue = true;
                    state.Current.Object = value;
                    return false;
                }

                if (c == ']')
                {
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
            else
            {
                Collection = (ICollection<TValue>)state.Current.Object!;
                value = (IReadOnlyCollection<TValue>)state.Current.Object!;
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
                            state.Current.Object = value;
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

                    if (!readConverter.TryReadFromParent(ref reader, ref state, Collection))
                    {
                        state.Current.HasReadFirstArrayElement = true;
                        state.Current.WorkingFirstChar = c;
                        state.Current.Object = value;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadFirstArrayElement = true;
                    state.Current.HasReadValue = true;
                    state.Current.Object = value;
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