// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if !NETSTANDARD2_0

using System.Buffers.Text;
using System.Globalization;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterTimeOnly : JsonConverter<TimeOnly>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TimeOnly value)
        {
            switch (token)
            {
                case JsonToken.String:
                    if (reader.UseBytes)
                    {
                        if (!Utf8Parser.TryParse(reader.ValueBytes, out TimeSpan parsed, out var consumed) || reader.ValueBytes.Length != consumed)
                            ThrowInvalidValue(ref reader);
                        value = TimeOnly.FromTimeSpan(parsed);
                        return true;
                    }
                    else
                    {
                        if (TimeSpan.TryParseExact(reader.ValueChars, "c", CultureInfo.InvariantCulture, out var timeSpan) && timeSpan.Ticks >= 0 && timeSpan.Ticks < TimeSpan.TicksPerDay)
                        {
                            value = TimeOnly.FromTimeSpan(timeSpan);
                            return true;
                        }
#if NETSTANDARD2_0
                        if (!TimeOnly.TryParse(reader.ValueChars.ToString(), out value))
#else
                        if (!TimeOnly.TryParse(reader.ValueChars, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
#endif
                            ThrowInvalidValue(ref reader);
                        return true;
                    }
                case JsonToken.Null:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.Number:
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TimeOnly value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}

#endif