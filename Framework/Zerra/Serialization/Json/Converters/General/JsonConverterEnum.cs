// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed class JsonConverterEnum<TParent, TValue> : JsonConverter<TParent, TValue>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value)
        {
            if (!typeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TParent, TValue>)} can only handle enum types.");

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
                    if (EnumName.TryParse(str, typeDetail.IsNullable ? typeDetail.InnerType : typeDetail.Type, out var parsed))
                    {
                        value = (TValue?)parsed;
                    }
                    else
                    {
                        if (state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = default;
                    }
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsUInt64(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    try
                    {
                        value = (TValue?)Enum.ToObject(typeDetail.IsNullable ? typeDetail.InnerType : typeDetail.Type, number);
                    }
                    catch
                    {
                        if (state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = default;
                    }
                    return true;
                case JsonValueType.Null_Completed:
                    if (!typeDetail.IsNullable && state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue? value)
        {
            if (value is null)
            {
                if (!writer.TryWriteNull(out state.SizeNeeded))
                    return false;
                return true;
            }

            if (!typeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TParent, TValue>)} can only handle enum types.");

            if (state.EnumAsNumber)
            {
                switch (typeDetail.EnumUnderlyingType.Value)
                {
                    case CoreEnumType.Byte:
                    case CoreEnumType.ByteNullable:
                        if (!writer.TryWrite((byte)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.SByte:
                    case CoreEnumType.SByteNullable:
                        if (!writer.TryWrite((sbyte)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int16:
                    case CoreEnumType.Int16Nullable:
                        if (!writer.TryWrite((short)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt16:
                    case CoreEnumType.UInt16Nullable:
                        if (!writer.TryWrite((ushort)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int32:
                    case CoreEnumType.Int32Nullable:
                        if (!writer.TryWrite((int)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt32:
                    case CoreEnumType.UInt32Nullable:
                        if (!writer.TryWrite((uint)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int64:
                    case CoreEnumType.Int64Nullable:
                        if (!writer.TryWrite((long)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt64:
                    case CoreEnumType.UInt64Nullable:
                        if (!writer.TryWrite((ulong)(object)value, out state.SizeNeeded))
                            return false;
                        return true;
                    default: throw new NotSupportedException();
                };
            }
            else
            {
                if (!writer.TryWriteQuoted(EnumName.GetName(typeDetail.IsNullable ? typeDetail.InnerType : typeDetail.Type, value), out state.SizeNeeded))
                    return false;
                return true;
            }
        }
    }
}