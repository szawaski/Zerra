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
                        str = reader.UnescapeStringBytes();
                    else
                        str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                    if (state.ErrorOnTypeMismatch && str.Length != 1)
                        ThrowCannotConvert(ref reader);
                    if (str.Length > 0)
                        value = (char)str[0];
                    else
                        value = default;
                    return true;
                case JsonToken.Null:
                    value = default;
                    return true;
                case JsonToken.Number:
                    if (reader.UseBytes)
                        str = reader.UnescapeStringBytes();
                    else
                        str = reader.ValueChars.ToString();
                    if (state.ErrorOnTypeMismatch && str.Length != 1)
                        ThrowCannotConvert(ref reader);
                    if (str.Length > 0)
                        value = (char)str[0];
                    else
                        value = default;
                    return true;
                case JsonToken.False:
                    value = default;
                    return true;
                case JsonToken.True:
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in char value)
            => writer.TryWriteEscapedQuoted(value, out state.SizeNeeded);
    }
}