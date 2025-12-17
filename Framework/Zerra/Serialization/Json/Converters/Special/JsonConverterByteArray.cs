// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterByteArray : JsonConverter<byte[]>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out byte[]? value)
        {
            if (token != JsonToken.String)
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }

            if (reader.UseBytes)
            {
                if (reader.StringBytes.Length == 0)
                {
                    value = Array.Empty<byte>();
                    return true;
                }
                value = Convert.FromBase64String(reader.UnescapeStringBytes());
            }
            else
            {
                if (reader.StringChars.Length == 0)
                {
                    value = Array.Empty<byte>();
                    return true;
                }
                value = Convert.FromBase64String(reader.StringChars.ToString());
            }
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in byte[] value)
        {
            string str;
            if (!state.Current.HasWrittenStart)
            {
                if (value.Length == 0)
                {
                    if (!writer.TryWriteEmptyString(out state.SizeNeeded))
                    {
                        return false;
                    }
                    return true;
                }

                str = Convert.ToBase64String(value);
            }
            else
            {
                str = (string)state.Current.Object!;
            }

            if (!writer.TryWriteQuoted(str, out state.SizeNeeded))
            {
                state.Current.Object = str;
                return false;
            }
            return true;
        }
    }
}