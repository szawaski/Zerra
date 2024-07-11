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
        private JsonConverter<ArrayAccessor<TValue>> converter = null!;

        private static TValue Getter(ArrayAccessor<TValue> parent) => parent.Get();
        private static void Setter(ArrayAccessor<TValue> parent, TValue value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = JsonConverterFactory<ArrayAccessor<TValue>>.Get(valueTypeDetail, null, Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, out TValue[]? value)
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
            ArrayAccessor<TValue> accessor;

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
                        value = Array.Empty<TValue>();
                        return true;
                    }

                    if (reader.TryPeakArrayLength(c, out var length))
                    {
                        accessor = new ArrayAccessor<TValue>(new TValue[length]);
                    }
                    else
                    {
                        accessor = new ArrayAccessor<TValue>();
                    }

                    reader.BackOne();

                    if (!converter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    accessor.Index++;
                    state.Current.WorkingFirstChar = null;
                }
                else
                {
                    accessor = (ArrayAccessor<TValue>)state.Current.Object!;
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
                    value = Array.Empty<TValue>();
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
            else
            {
                accessor = (ArrayAccessor<TValue>)state.Current.Object!;
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

                    if (!converter.TryReadFromParent(ref reader, ref state, accessor))
                    {
                        state.Current.HasReadFirstArrayElement = true;
                        state.Current.WorkingFirstChar = c;
                        value = default;
                        return false;
                    }
                    accessor.Index++;
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

            value = accessor.ToArray();
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, TValue[] value)
        {
            if (value.Length == 0)
            {
                if (!writer.TryWrite("[]", out state.CharsNeeded))
                {
                    return false;
                }
                return true;
            }

            ArrayAccessor<TValue> accessor;

            if (!state.Current.HasWrittenStart)
            {
                if (!writer.TryWrite('[', out state.CharsNeeded))
                {
                    return false;
                }
                accessor = new ArrayAccessor<TValue>(value);
            }
            else
            {
                accessor = (ArrayAccessor<TValue>)state.Current.Object!;
            }

            if (!state.Current.HasWrittenFirst)
            {
                if (!converter.TryWriteFromParent(ref writer, ref state, accessor))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
            }

            if (accessor.Index == accessor.Length)
            {
                if (!writer.TryWrite(']', out state.CharsNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }

            for (; ; )
            {
                if (!state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWrite(',', out state.CharsNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.HasWrittenFirst = true;
                        return false;
                    }
                }

                if (!converter.TryWriteFromParent(ref writer, ref state, accessor))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.HasWrittenFirst = true;
                    state.Current.HasWrittenSeperator = true;
                    return false;
                }

                if (accessor.Index == accessor.Length)
                {
                    if (!writer.TryWrite(']', out state.CharsNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        return false;
                    }
                    return true;
                }
                state.Current.HasWrittenSeperator = false;
            }
        }
    }
}