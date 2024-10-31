// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Lists
{
    internal sealed class JsonConverterISetTOfT<TParent, TSet, TValue> : JsonConverter<TParent, TSet>
    {
        private JsonConverter<ISet<TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ISet<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<ISet<TValue>>.Get(valueTypeDetail, nameof(JsonConverterISetTOfT<TParent, TSet, TValue>), null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, nameof(JsonConverterISetTOfT<TParent, TSet, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TSet? value)
        {
            if (valueType != JsonValueType.Array)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            ISet<TValue> set;
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
                    value = typeDetail.Creator();
                    return true;
                }

                reader.BackOne();

                value = typeDetail.Creator();
                set = (ISet<TValue>)value!;
            }
            else
            {
                value = (TSet)state.Current.Object!;
                set = (ISet<TValue>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!readConverter.TryReadFromParent(ref reader, ref state, set))
                    {
                        state.Current.HasCreated = true;
                        state.Current.Object = set;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    state.Current.Object = set;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TSet value)
        {
            IEnumerator<TValue> enumerator;

            if (!state.Current.HasWrittenStart)
            {
                var set = (ISet<TValue>)value!;
                if (set.Count == 0)
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
                enumerator = set.GetEnumerator();
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