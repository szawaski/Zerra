﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers.Text;
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
                case JsonValueType.Number:
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out value, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                    else
                    {
                        if (!reader.TryReadNumberChars(out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Byte.TryParse(chars.ToString(), out value) && state.ErrorOnTypeMismatch)
#else
                        if (!Byte.TryParse(chars, out value) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                case JsonValueType.String:
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadStringQuotedBytes(true, out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out value, out var consumed) || bytes.Length != consumed) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
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
                        if (!Byte.TryParse(chars.ToString(), out value) && state.ErrorOnTypeMismatch)
#else
                        if (!Byte.TryParse(chars, out value) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        return true;
                    }
                case JsonValueType.Null_Completed:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = (byte)0;
                    return true;
                case JsonValueType.True_Completed:
                    value = (byte)1;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in byte value)
            => writer.TryWrite(value, out state.SizeNeeded);
    }
}