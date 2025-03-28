﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Collections
{
    internal sealed class JsonConverterICollection<TParent> : JsonConverter<TParent, ICollection>
    {
        private JsonConverter<ArrayOrListAccessor<object>> readConverter = null!;
        private JsonConverter<IEnumerator> writeConverter = null!;

        private static object Getter(IEnumerator parent) => parent.Current;
        private static void Setter(ArrayOrListAccessor<object> parent, object value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            readConverter = JsonConverterFactory<ArrayOrListAccessor<object>>.Get(valueTypeDetail, nameof(JsonConverterICollection<TParent>), null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator>.Get(valueTypeDetail, nameof(JsonConverterICollection<TParent>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out ICollection? value)
        {
            if (valueType != JsonValueType.Array)
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            ArrayOrListAccessor<object> accessor;
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = Array.Empty<object>();
                    return true;
                }

                reader.BackOne();

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
                    state.SizeNeeded = 1;
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

                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator))
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