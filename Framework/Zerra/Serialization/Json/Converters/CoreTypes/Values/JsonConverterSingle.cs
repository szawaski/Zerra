// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
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
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out value, out var consumed) || consumed != reader.ValueBytes.Length)
                            ThrowInvalidValue(ref reader);
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Single.TryParse(reader.ValueChars.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!Single.TryParse(reader.ValueChars, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out value))
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
                        if (!Single.TryParse(reader.ValueChars.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out value))
#else
                        if (!Single.TryParse(reader.ValueChars, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out value))
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in float value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}