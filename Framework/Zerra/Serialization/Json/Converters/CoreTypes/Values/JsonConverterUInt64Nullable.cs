// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterUInt64Nullable : JsonConverter<ulong?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out ulong? value)
        {
            switch (token)
            {
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out ulong parsed, out var consumed) || consumed != reader.ValueBytes.Length)
                        {
                            if (state.ErrorOnReadMismatchedData || !reader.IsNumberValid())
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                    else
                    {
                        var chars = reader.ValueChars;
                        if (chars.Length <= 19)
                        {
                            ulong result = 0;
                            var index = 0;
                            for (; index < chars.Length; index++)
                            {
                                var digit = (uint)(chars[index] - '0');
                                if (digit > 9)
                                    break;
                                result = result * 10 + digit;
                            }
                            if (index == chars.Length)
                            {
                                value = result;
                                return true;
                            }
                        }
#if NETSTANDARD2_0
                        if (!UInt64.TryParse(reader.ValueChars.ToString(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out ulong parsed))
#else
                        if (!UInt64.TryParse(reader.ValueChars, NumberStyles.None, NumberFormatInfo.InvariantInfo, out ulong parsed))
#endif
                        {
                            if (state.ErrorOnReadMismatchedData || !reader.IsNumberValid())
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (reader.ValueBytes.Length == 0)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = null;
                            return true;
                        }
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out ulong parsed, out var consumed) || reader.ValueBytes.Length != consumed)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                    else
                    {
                        if (reader.ValueChars.Length == 0)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = null;
                            return true;
                        }
#if NETSTANDARD2_0
                        if (!UInt64.TryParse(reader.ValueChars.ToString(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out ulong parsed))
#else
                        if (!UInt64.TryParse(reader.ValueChars, NumberStyles.None, NumberFormatInfo.InvariantInfo, out ulong parsed))
#endif
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                case JsonToken.Null:
                    value = null;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in ulong? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWrite(value.Value, out state.SizeNeeded);
    }
}