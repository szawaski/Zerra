// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterBooleanNullable : JsonConverter<bool?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out bool? value)
        {
            switch (token)
            {
                case JsonToken.False:
                    value = false;
                    return true;
                case JsonToken.True:
                    value = true;
                    return true;
                case JsonToken.Null:
                    value = null;
                    return true;
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out bool parsed, out var consumed) || reader.ValueBytes.Length != consumed) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Boolean.TryParse(reader.ValueChars.ToString(), out bool parsed) && state.ErrorOnTypeMismatch)
#else
                        if (!Boolean.TryParse(reader.ValueChars, out bool parsed) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out float number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = number > 0;
                        return true;
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Single.TryParse(reader.ValueChars.ToString(), out var number) && state.ErrorOnTypeMismatch)
#else
                        if (!Single.TryParse(reader.ValueChars, out var number) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        value = number > 0;
                        return true;
                    }
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in bool? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : (value == true ? writer.TryWriteTrue(out state.SizeNeeded) : writer.TryWriteFalse(out state.SizeNeeded));
    }
}