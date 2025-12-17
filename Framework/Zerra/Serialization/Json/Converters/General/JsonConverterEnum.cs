// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed class JsonConverterEnum<TValue> : JsonConverter<TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue? value)
        {
            if (!TypeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TValue>)} can only handle enum types.");

            switch (token)
            {
                case JsonToken.String:
                    string str;
                    if (reader.UseBytes)
                        str = reader.UnescapeStringBytes();
                    else
                        str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                    if (EnumName.TryParse(str, TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, out var parsed))
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
                case JsonToken.Number:
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out long number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        try
                        {
                            value = (TValue?)Enum.ToObject(TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, number);
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
#if NETSTANDARD2_0
                        if (!UInt64.TryParse(reader.ValueChars.ToString(), out var number) && state.ErrorOnTypeMismatch)
#else
                        if (!UInt64.TryParse(reader.ValueChars, out var number) && state.ErrorOnTypeMismatch)
#endif
                            ThrowCannotConvert(ref reader);
                        try
                        {
                            value = (TValue?)Enum.ToObject(TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, number);
                        }
                        catch
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            value = default;
                        }
                        return true;
                    }
                case JsonToken.Null:
                    if (!TypeDetail.IsNullable && state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.False:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.True:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
                case JsonToken.ObjectStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonToken.ArrayStart:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return DrainArray(ref reader, ref state);
                default:
                    throw reader.CreateException();
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

            if (!TypeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TValue>)} can only handle enum types.");

            if (state.EnumAsNumber)
            {
                switch (TypeDetail.EnumUnderlyingType.Value)
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
                if (!writer.TryWriteQuoted(EnumName.GetName(TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, value), out state.SizeNeeded))
                    return false;
                return true;
            }
        }
    }
}