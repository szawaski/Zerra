// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterBoolean<TParent> : JsonConverter<TParent, bool>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out bool value)
        {
            switch (state.Current.ValueType)
            {
                case JsonValueType.Object:
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    value = default;
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String:
                    if (!ReadString(ref reader, ref state, out var str))
                    {
                        value = default;
                        return false;
                    }
                    _ = Boolean.TryParse(str, out value);
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsDouble(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = number > 0;
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = false;
                    return true;
                case JsonValueType.True_Completed:
                    value = true;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, bool value)
            => writer.TryWrite(value == true ? "true" : "false", out state.CharsNeeded);
    }
}