// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using Zerra.SourceGeneration.Types;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed class JsonConverterEnum<TValue> : JsonConverter<TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value)
        {
            if (!typeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TValue>)} can only handle enum types.");

            switch (valueType)
            {
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    if (EnumName.TryParse(str, typeDetail.IsNullable ? typeDetail.InnerType! : typeDetail.Type, out var parsed))
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
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }

                        if ((!Utf8Parser.TryParse(bytes, out long number, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        try
                        {
                            value = (TValue?)Enum.ToObject(typeDetail.IsNullable ? typeDetail.InnerType! : typeDetail.Type, number);
                        }
                        catch
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = default;
                        }
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
                        if (!UInt64.TryParse(chars.ToString(), out var number) && state.ErrorOnTypeMismatch)
#else
                        if (!UInt64.TryParse(chars, out var number) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        try
                        {
                            value = (TValue?)Enum.ToObject(typeDetail.IsNullable ? typeDetail.InnerType! : typeDetail.Type, number);
                        }
                        catch
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = default;
                        }
                        return true;
                    }
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue? value)
        {
            if (value is null)
            {
                if (!writer.TryWriteNull(out state.SizeNeeded))
                    return false;
                return true;
            }

            if (!typeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TValue>)} can only handle enum types.");

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
                if (!writer.TryWriteQuoted(EnumName.GetName(typeDetail.IsNullable ? typeDetail.InnerType! : typeDetail.Type, value), out state.SizeNeeded))
                    return false;
                return true;
            }
        }
    }
}