﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterInt32Nullable<TParent> : JsonConverter<TParent, int?>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out int? value)
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
                    if (!ReadNumberAsInt64(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = (int)number;
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = 0;
                    return true;
                case JsonValueType.True_Completed:
                    value = 1;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, int? value)
            => value is null ? writer.TryWrite("null", out state.CharsNeeded) : writer.TryWrite(value.Value, out state.CharsNeeded);
    }
}