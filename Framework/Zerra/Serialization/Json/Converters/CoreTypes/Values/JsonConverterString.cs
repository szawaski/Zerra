// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterString : JsonConverter<string?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out string? value)
        {
            switch (token)
            {
                case JsonToken.String:
                    if (reader.UseBytes)
                        value = reader.UnescapeStringBytes();
                    else
                        value = reader.StringPositionOfFirstEscape == -1 ? reader.StringChars.ToString() : reader.UnescapeStringChars();
                    return true;
                case JsonToken.Null:
                    value = null;
                    return true;
                case JsonToken.Number:
                    if (reader.UseBytes)
                        value = System.Text.Encoding.UTF8.GetString(reader.StringBytes);
                    else
                        value = reader.StringChars.ToString();
                    return true;
                case JsonToken.False:
                    value = "false";
                    return true;
                case JsonToken.True:
                    value = "true";
                    return true;
                case JsonToken.ObjectStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = String.Empty;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in string? value)
            => writer.TryWriteEscapedQuoted(value, out state.SizeNeeded);
    }
}