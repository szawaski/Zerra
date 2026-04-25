// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterInt64Nullable : JsonConverter<long?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out long? value)
        {
            switch (token)
            {
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out long parsed, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Int64.TryParse(reader.ValueChars.ToString(), out long parsed) && state.ErrorOnTypeMismatch)
#else
                        if (!Int64.TryParse(reader.ValueChars, out long parsed) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (reader.ValueBytes.Length == 0)
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = null;
                            return true;
                        }
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out long parsed, out var consumed) || reader.ValueBytes.Length != consumed) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                    else
                    {
                        if (reader.ValueChars.Length == 0)
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = null;
                            return true;
                        }
#if NETSTANDARD2_0
                        if (!Int64.TryParse(reader.ValueChars.ToString(), out long parsed) && state.ErrorOnTypeMismatch)
#else
                        if (!Int64.TryParse(reader.ValueChars, out long parsed) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                case JsonToken.Null:
                    value = null;
                    return true;
                case JsonToken.False:
                    value = 0L;
                    return true;
                case JsonToken.True:
                    value = 1L;
                    return true;
                case JsonToken.ObjectStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainArray(ref reader, ref state);
                default:
                    throw reader.CreateException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in long? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWrite(value.Value, out state.SizeNeeded);
    }
}