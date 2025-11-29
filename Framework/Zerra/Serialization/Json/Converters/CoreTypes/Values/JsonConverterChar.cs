// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterChar : JsonConverter<char>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out char value)
        {
            switch (valueType)
            {
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    if (state.ErrorOnTypeMismatch && str.Length != 1)
                        ThrowCannotConvert(ref reader);
                    value = str[0];
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.Number:
                    if (!reader.TryReadNumberString(out str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    if (state.ErrorOnTypeMismatch && str.Length != 1)
                        ThrowCannotConvert(ref reader);
                    value = str[0];
                    return true;
                case JsonValueType.False_Completed:
                    value = default;
                    return true;
                case JsonValueType.True_Completed:
                    value = default;
                    return true;
                case JsonValueType.Object:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainArray(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in char value)
            => writer.TryWriteEscapedQuoted(value, out state.SizeNeeded);
    }
}