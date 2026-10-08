// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterChar : JsonConverter<char>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out char value)
        {
            switch (token)
            {
                case JsonToken.String:
                    string str;
                    if (reader.UseBytes)
                    {
                        if (reader.ValueBytes.Length == 1 && reader.ValueBytes[0] < 0x80)
                        {
                            value = (char)reader.ValueBytes[0];
                            return true;
                        }
                        str = reader.UnescapeStringBytes();
                    }
                    else
                    {
                        if (reader.ValueChars.Length == 1)
                        {
                            value = reader.ValueChars[0];
                            return true;
                        }
                        str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                    }
                    if (str.Length != 1)
                    {
                        if (state.ErrorOnReadMismatchedData)
                            ThrowInvalidValue(ref reader);
                        value = default;
                        return true;
                    }
                    value = str[0];
                    return true;
                case JsonToken.Null:
                    value = default;
                    return true;
                case JsonToken.Number:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    if (reader.UseBytes ? reader.ValueBytes.Length != 1 : reader.ValueChars.Length != 1)
                    {
                        value = default;
                        return true;
                    }
                    value = reader.UseBytes ? (char)reader.ValueBytes[0] : reader.ValueChars[0];
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in char value)
            => writer.TryWriteEscapedQuoted(value, out state.SizeNeeded);
    }
}