// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterUInt32Nullable<TParent> : JsonConverter<TParent, uint?>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out uint? value)
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
                case JsonValueType.Number:
                    if (!ReadNumberAsUInt64(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = (uint)number;
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = 0U;
                    return true;
                case JsonValueType.True_Completed:
                    value = 1U;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, uint? value)
            => value is null ? writer.TryWrite("null", out state.CharsNeeded) : writer.TryWrite(value.Value, out state.CharsNeeded);
    }
}