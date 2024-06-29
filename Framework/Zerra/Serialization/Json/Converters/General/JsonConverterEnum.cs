// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed class JsonConverterEnum<TParent, TValue> : JsonConverter<TParent, TValue>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out TValue? value)
        {
            if (!typeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TParent, TValue>)} can only handle enum types.");

            switch (state.Current.ValueType)
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
                    if (!ReadString(ref reader, ref state, out var str))
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
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
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
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        value = default;
                    }
                    return true;
                case JsonValueType.Null_Completed:
                    if (!typeDetail.IsNullable && state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return true;
                case JsonValueType.True_Completed:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    value = default;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, TValue? value)
        {
            if (value == null)
            {
                if (!writer.TryWrite("null", out state.CharsNeeded))
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
                        if (!writer.TryWrite((byte)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.SByte:
                    case CoreEnumType.SByteNullable:
                        if (!writer.TryWrite((sbyte)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int16:
                    case CoreEnumType.Int16Nullable:
                        if (!writer.TryWrite((short)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt16:
                    case CoreEnumType.UInt16Nullable:
                        if (!writer.TryWrite((ushort)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int32:
                    case CoreEnumType.Int32Nullable:
                        if (!writer.TryWrite((int)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt32:
                    case CoreEnumType.UInt32Nullable:
                        if (!writer.TryWrite((uint)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int64:
                    case CoreEnumType.Int64Nullable:
                        if (!writer.TryWrite((long)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt64:
                    case CoreEnumType.UInt64Nullable:
                        if (!writer.TryWrite((ulong)(object)value, out state.CharsNeeded))
                            return false;
                        return true;
                    default: throw new NotSupportedException();
                };
            }
            else
            {
                if (!writer.TryWrite(EnumName.GetName(typeDetail.Type, value), out state.CharsNeeded))
                    return false;
                return true;
            }
        }
    }
}