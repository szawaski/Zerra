// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterInt64 : JsonConverter<long>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out long value)
        {
            switch (token)
            {
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out value, out var consumed) || consumed != reader.ValueBytes.Length)
                        {
                            if (state.ErrorOnReadMismatchedData || !reader.IsNumberValid())
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                    else
                    {
                        var chars = reader.ValueChars;
                        var negative = chars[0] == '-';
                        var index = negative ? 1 : 0;
                        if (chars.Length - index is > 0 and <= 18)
                        {
                            long result = 0;
                            for (; index < chars.Length; index++)
                            {
                                var digit = (uint)(chars[index] - '0');
                                if (digit > 9)
                                    break;
                                result = result * 10 + (long)digit;
                            }
                            if (index == chars.Length)
                            {
                                value = negative ? -result : result;
                                return true;
                            }
                        }
#if NETSTANDARD2_0
                        if (!Int64.TryParse(reader.ValueChars.ToString(), NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!Int64.TryParse(reader.ValueChars, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out value))
#endif
                        {
                            if (state.ErrorOnReadMismatchedData || !reader.IsNumberValid())
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out value, out var consumed) || reader.ValueBytes.Length != consumed)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Int64.TryParse(reader.ValueChars.ToString(), NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!Int64.TryParse(reader.ValueChars, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out value))
#endif
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                case JsonToken.Null:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.False:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.True:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.ObjectStart:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainArray(ref reader, ref state);
                default:
                    throw reader.CreateException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in long value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}