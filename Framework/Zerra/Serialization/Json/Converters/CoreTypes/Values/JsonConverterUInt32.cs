// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterUInt32 : JsonConverter<uint>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out uint value)
        {
            switch (token)
            {
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out value, out var consumed) || consumed != reader.ValueBytes.Length)
                            ThrowInvalidValue(ref reader);
                        return true;
                    }
                    else
                    {
                        var chars = reader.ValueChars;
                        if (chars.Length <= 9)
                        {
                            uint result = 0;
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
                        if (!UInt32.TryParse(reader.ValueChars.ToString(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!UInt32.TryParse(reader.ValueChars, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value))
#endif
                            ThrowInvalidValue(ref reader);
                        return true;
                    }
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out value, out var consumed) || reader.ValueBytes.Length != consumed)
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!UInt32.TryParse(reader.ValueChars.ToString(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!UInt32.TryParse(reader.ValueChars, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value))
#endif
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = default;
                            return true;
                        }
                        return true;
                    }
                case JsonToken.Null:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.False:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.True:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in uint value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}