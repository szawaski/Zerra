﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterSByteNullable<TParent> : JsonConverter<TParent, sbyte?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out sbyte? value)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String:
                    if (!ReadStringAsInt64(ref reader, ref state, out var numberFromString))
                    {
                        value = default;
                        return false;
                    }
                    value = (sbyte?)numberFromString;
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsInt64(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = (sbyte)number;
                    return true;
                case JsonValueType.Null_Completed:
                    value = null;
                    return true;
                case JsonValueType.False_Completed:
                    value = (sbyte)0;
                    return true;
                case JsonValueType.True_Completed:
                    value = (sbyte)1;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in sbyte? value)
            => value is null ? writer.TryWriteNull(out state.CharsNeeded) : writer.TryWrite(value.Value, out state.CharsNeeded);
    }
}