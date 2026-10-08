// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterGuidNullable : JsonConverter<Guid?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out Guid? value)
        {
            switch (token)
            {
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out Guid parsed, out var consumed) || reader.ValueBytes.Length != consumed)
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Guid.TryParse(reader.ValueChars.ToString(), out Guid parsed))
#else
                        if (!Guid.TryParse(reader.ValueChars, out Guid parsed))
#endif
                        {
                            if (state.ErrorOnReadMismatchedData)
                                ThrowInvalidValue(ref reader);
                            value = default;
                            return true;
                        }
                        value = parsed;
                        return true;
                    }
                case JsonToken.Null:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.Number:
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in Guid? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWrite(value.Value, out state.SizeNeeded);
    }
}