// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Test.Helpers.Models
{
    public class CustomTypeJsonConverter : JsonConverter<CustomType>
    {
        protected override bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out CustomType? value)
        {
            switch (token)
            {
                case JsonToken.String:
                    string str;
                    if (reader.UseBytes)
                        str = reader.UnescapeStringBytes();
                    else
                        str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                    var values = str.Split(" - ");
                    value = new CustomType()
                    {
                        Things1 = values[0],
                        Things2 = values.Length > 0 ? values[1] : null
                    };
                    return true;
                case JsonToken.Null:
                    value = null;
                    return true;
                case JsonToken.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonToken.False:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonToken.True:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonToken.ObjectStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainArray(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }

        protected override bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in CustomType value)
        {
            var str = $"{value.Things1} - {value.Things2}";
            return writer.TryWriteEscapedQuoted(str, out state.SizeNeeded);
        }
    }
}
