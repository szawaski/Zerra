// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterString<TParent> : JsonConverter<TParent, string?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out string? value)
        {
            switch (valueType)
            {
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out value, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    return true;
                case JsonValueType.Null_Completed:
                    value = null;
                    return true;
                case JsonValueType.Number:
                    if (!reader.TryReadNumberString(out value, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    return true;
                case JsonValueType.False_Completed:
                    value = "false";
                    return true;
                case JsonValueType.True_Completed:
                    value = "true";
                    return true;
                case JsonValueType.Object:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = String.Empty;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in string? value)
            => writer.TryWriteEscapedQuoted(value, out state.SizeNeeded);
    }
}