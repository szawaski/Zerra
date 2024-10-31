﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterBooleanNullable<TParent> : JsonConverter<TParent, bool?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out bool? value)
        {
            switch (valueType)
            {
                case JsonValueType.False_Completed:
                    value = false;
                    return true;
                case JsonValueType.True_Completed:
                    value = true;
                    return true;
                case JsonValueType.Null_Completed:
                    value = null;
                    return true;
                case JsonValueType.String:
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadStringQuotedBytes(true, out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out bool parsed, out var consumed) || bytes.Length != consumed) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                    else
                    {
                        if (!reader.TryReadStringQuotedChars(true, out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Boolean.TryParse(chars.ToString(), out bool parsed) && state.ErrorOnTypeMismatch)
#else
                        if (!Boolean.TryParse(chars, out bool parsed) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        value = parsed;
                        return true;
                    }
                case JsonValueType.Number:
                    if (!ReadNumberAsDouble(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = number > 0;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in bool? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : (value == true ? writer.TryWriteTrue(out state.SizeNeeded) : writer.TryWriteFalse(out state.SizeNeeded));
    }
}