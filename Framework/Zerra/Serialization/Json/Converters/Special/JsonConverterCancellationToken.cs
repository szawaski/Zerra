// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterCancellationToken : JsonConverter<CancellationToken>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out CancellationToken value)
        {
            switch (token)
            {
                case JsonToken.ObjectStart:
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainArray(ref reader, ref state);
                case JsonToken.String:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
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
                default:
                    throw reader.CreateException();
            }
        }

        private static readonly char[] emptyObjectChars = ['{', '}'];
        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in CancellationToken value)
            => writer.TryWrite(emptyObjectChars, out state.SizeNeeded);
    }
}