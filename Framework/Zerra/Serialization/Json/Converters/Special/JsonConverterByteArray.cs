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
                if (state.ErrorOnReadMismatchedData)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, token);
            }

            string str;
            if (reader.UseBytes)
            {
                if (reader.ValueBytes.Length == 0)
                {
                    value = Array.Empty<byte>();
                    return true;
                }
                str = reader.UnescapeStringBytes();
            }
            else
            {
                if (reader.ValueChars.Length == 0)
                {
                    value = Array.Empty<byte>();
                    return true;
                }
                str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
            }

#if NETSTANDARD2_0
            try
            {
                value = Convert.FromBase64String(str);
                return true;
            }
            catch (FormatException)
            {
            }
#else
            var size = str.Length / 4 * 3;
            if (str.Length > 0 && str[str.Length - 1] == '=')
                size--;
            if (str.Length > 1 && str[str.Length - 2] == '=')
                size--;
            var buffer = new byte[Math.Max(size, 0)];
            if (Convert.TryFromBase64String(str, buffer, out var written))
            {
                value = written == buffer.Length ? buffer : buffer.AsSpan(0, written).ToArray();
                return true;
            }
#endif

            if (state.ErrorOnReadMismatchedData)
                ThrowInvalidValue(ref reader);
            value = default;
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