﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterCharNullable<TParent> : JsonConverter<TParent, char?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out char? value)
        {
            switch (valueType)
            {
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
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    if (Char.TryParse(str, out var parsed))
                    {
                        value = parsed;
                    }
                    else
                    {
                        if (state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = default;
                    }
                    return true;
                case JsonValueType.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainNumber(ref reader, ref state);
                case JsonValueType.Null_Completed:
                    value = null;
                    return true;
                case JsonValueType.False_Completed:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonValueType.True_Completed:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in char? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWriteEscapedQuoted(value.Value, out state.SizeNeeded);
    }
}