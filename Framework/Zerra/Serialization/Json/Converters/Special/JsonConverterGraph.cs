// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    //a graph keeps its members in private fields so it is written as the signature and read back by parsing it
    internal sealed class JsonConverterGraph<TValue> : JsonConverter<TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue? value)
        {
            switch (token)
            {
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
                case JsonToken.String:
                    string signature;
                    if (reader.UseBytes)
                        signature = reader.UnescapeStringBytes();
                    else
                        signature = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();

                    var graph = TypeDetail.Creator!(); //graph may be typed so has to create here
                    if (!Graph.TryParseSignature(signature, (Graph)(object)graph!))
                    {
                        if (state.ErrorOnReadMismatchedData)
                            ThrowInvalidValue(ref reader);
                        value = default;
                        return true;
                    }
                    value = graph;
                    return true;
                case JsonToken.Number:
                    if (state.ErrorOnReadMismatchedData)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.Null:
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
                default:
                    throw reader.CreateException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWriteQuoted(((Graph)(object)value).Signature, out state.SizeNeeded);
    }
}
