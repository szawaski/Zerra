// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterCharNullable : JsonConverter<char?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out char? value)
        {
            switch (token)
            {
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (state.ErrorOnTypeMismatch && reader.ValueBytes.Length != 1)
                            ThrowCannotConvert(ref reader);
                        value = (char)reader.ValueBytes[0];
                    }
                    else
                    {
                        if (state.ErrorOnTypeMismatch && reader.ValueChars.Length != 1)
                            ThrowCannotConvert(ref reader);
                        value = reader.ValueChars[0];
                    }
                    return true;
                case JsonToken.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.Null:
                    value = null;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in char? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWriteEscapedQuoted(value.Value, out state.SizeNeeded);
    }
}