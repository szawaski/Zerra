// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterSingle : JsonConverter<float>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out float value)
        {
            switch (token)
            {
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.StringBytes, out value, out var consumed) || consumed != reader.StringBytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Single.TryParse(reader.ValueChars.ToString(), out value) && state.ErrorOnTypeMismatch)
#else
                        if (!Single.TryParse(reader.StringChars, out value) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.StringBytes, out value, out var consumed) || reader.StringBytes.Length != consumed) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Single.TryParse(reader.ValueChars.ToString(), out value) && state.ErrorOnTypeMismatch)
#else
                        if (!Single.TryParse(reader.StringChars, out value) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                case JsonToken.Null:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.False:
                    value = 0f;
                    return true;
                case JsonToken.True:
                    value = 1f;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in float value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}