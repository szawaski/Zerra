// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.IO;
using Zerra.Serialization.Json.State;
using System.Collections.Generic;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class JsonConverterArrayT<TParent, TValue> : JsonConverter<TParent, TValue[]>
    {
        private JsonConverter<List<TValue>> readConverter = null!;
        private JsonConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(List<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = JsonConverterFactory<List<TValue>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out TValue[]? value)
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

            char c;
            List<TValue> accessor;

            if (!state.Current.HasReadFirstArrayElement)
            {
                if (!state.Current.HasReadValue)
                {
                    if (!state.Current.WorkingFirstChar.HasValue)
                    {
                        if (!reader.TryReadSkipWhiteSpace(out c))
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
                        value = Array.Empty<TValue>();
                        return true;
                    }

                    accessor = new List<TValue>();

                    reader.BackOne();

                    state.PushFrame();
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
                    if (state.Current.Object is null)
                    {
                        accessor = new List<TValue>();
                    }
                    else
                    {
                        accessor = (List<TValue>)state.Current.Object!;
                    }
                }

                if (!reader.TryReadSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = Array.Empty<TValue>();
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
            else
            {
                if (state.Current.Object is null)
                {
                    accessor = new List<TValue>();
                }
                else
                {
                    accessor = (List<TValue>)state.Current.Object!;
                }
            }


            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!state.Current.WorkingFirstChar.HasValue)
                    {
                        if (!reader.TryReadSkipWhiteSpace(out c))
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
                    {
                        value = Array.Empty<TValue>();
                        return true;
                    }

                    reader.BackOne();

                    state.PushFrame();
                    if (!readConverter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.HasReadFirstArrayElement = true;
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    state.Current.WorkingFirstChar = null;
                }

                if (!reader.TryReadSkipWhiteSpace(out c))
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

            value = accessor.ToArray();
            return true;
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, TValue[]? value)
        {
            throw new NotImplementedException();
        }
    }
}