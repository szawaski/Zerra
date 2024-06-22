// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterUInt64<TParent> : JsonConverter<TParent, ulong>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out ulong value)
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
                    value = number;
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = 0UL;
                    return true;
                case JsonValueType.True_Completed:
                    value = 1UL;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, ulong value)
            => writer.TryWrite(value, out state.CharsNeeded);
    }
}