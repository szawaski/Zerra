﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterByte<TParent> : JsonConverter<TParent, byte>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out byte value)
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
                case JsonValueType.Number:
                    if (!ReadNumberAsUInt64(ref reader, ref state, valueType, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = (byte)number;
                    return true;
                case JsonValueType.Null_Completed:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = (byte)0;
                    return true;
                case JsonValueType.True_Completed:
                    value = (byte)1;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, byte value)
            => writer.TryWrite(value, out state.CharsNeeded);
    }
}