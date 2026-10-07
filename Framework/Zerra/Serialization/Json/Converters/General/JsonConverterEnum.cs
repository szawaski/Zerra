// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed class JsonConverterEnum<TValue> : JsonConverter<TValue>
    {
        protected override bool StackRequired => false;

        private Dictionary<TValue, (char[] Chars, byte[] Bytes)> quotedNames = new();
        private Dictionary<string, TValue?> valuesByName = new();
        private const int maxCachedNames = 256;
        private const int maxStackNameLength = 128;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue? value)
        {
            if (!TypeDetail.EnumUnderlyingType.HasValue)
                throw new InvalidOperationException($"{nameof(JsonConverterEnum<TValue>)} can only handle enum types.");

            switch (token)
            {
                case JsonToken.String:
#if !NETSTANDARD2_0
                    if (reader.PositionOfFirstEscape == -1)
                    {
                        if (reader.UseBytes)
                        {
                            if (reader.ValueBytes.Length <= maxStackNameLength)
                            {
                                Span<char> nameChars = stackalloc char[maxStackNameLength];
                                var nameLength = Encoding.UTF8.GetChars(reader.ValueBytes, nameChars);
                                if (valuesByName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(nameChars.Slice(0, nameLength), out value))
                                    return true;
                            }
                        }
                        else
                        {
                            if (valuesByName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(reader.ValueChars, out value))
                                return true;
                        }
                    }
#endif
                    string str;
                    if (reader.UseBytes)
                        str = reader.UnescapeStringBytes();
                    else
                        str = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars();
                    if (EnumName.TryParse(str, TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, out var parsed))
                    {
                        value = (TValue?)parsed;
                        //replaced instead of changed so concurrent reads need no lock, capped so unusual input can't grow it
                        if (valuesByName.Count < maxCachedNames)
                            valuesByName = new Dictionary<string, TValue?>(valuesByName) { [str] = value };
                    }
                    else if (!TryParseNumber(str.AsSpan(), out value))
                    {
                        if (state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = default;
                    }
                    return true;
                case JsonToken.Number:
                    if (reader.UseBytes ? TryParseNumber(reader.ValueBytes, out value) : TryParseNumber(reader.ValueChars, out value))
                        return true;
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = default;
                    return true;
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
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (byte)(object)value! : Unsafe.As<TValue, byte>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.SByte:
                    case CoreEnumType.SByteNullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (sbyte)(object)value! : Unsafe.As<TValue, sbyte>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int16:
                    case CoreEnumType.Int16Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (short)(object)value! : Unsafe.As<TValue, short>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt16:
                    case CoreEnumType.UInt16Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (ushort)(object)value! : Unsafe.As<TValue, ushort>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int32:
                    case CoreEnumType.Int32Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (int)(object)value! : Unsafe.As<TValue, int>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt32:
                    case CoreEnumType.UInt32Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (uint)(object)value! : Unsafe.As<TValue, uint>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.Int64:
                    case CoreEnumType.Int64Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (long)(object)value! : Unsafe.As<TValue, long>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    case CoreEnumType.UInt64:
                    case CoreEnumType.UInt64Nullable:
                        if (!writer.TryWrite(TypeDetail.IsNullable ? (ulong)(object)value! : Unsafe.As<TValue, ulong>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                            return false;
                        return true;
                    default: throw new NotSupportedException();
                };
            }
            else
            {
                if (!quotedNames.TryGetValue(value, out var quotedName))
                {
                    var escaped = StringHelper.EscapeString(EnumName.GetName(TypeDetail.IsNullable ? TypeDetail.InnerType! : TypeDetail.Type, value), false)!;
                    var chars = new char[escaped.Length + 2];
                    chars[0] = '"';
                    escaped.CopyTo(chars, 1);
                    chars[chars.Length - 1] = '"';
                    quotedName = (chars, Encoding.UTF8.GetBytes(chars));

                    //replaced instead of changed so concurrent reads need no lock
                    quotedNames = new Dictionary<TValue, (char[], byte[])>(quotedNames) { [value] = quotedName };
                }

                if (writer.UseBytes)
                {
                    if (!writer.TryWriteNameSegment(quotedName.Bytes, false, out state.SizeNeeded))
                        return false;
                }
                else
                {
                    if (!writer.TryWriteNameSegment(quotedName.Chars, false, out state.SizeNeeded))
                        return false;
                }
                return true;
            }
        }

        private bool TryParseNumber(ReadOnlySpan<byte> bytes, out TValue? value)
        {
            int consumed;
            switch (TypeDetail.EnumUnderlyingType!.Value)
            {
                case CoreEnumType.Byte:
                case CoreEnumType.ByteNullable:
                    if (Utf8Parser.TryParse(bytes, out byte byteNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, byteNumber) : Unsafe.As<byte, TValue>(ref byteNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.SByte:
                case CoreEnumType.SByteNullable:
                    if (Utf8Parser.TryParse(bytes, out sbyte sbyteNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, sbyteNumber) : Unsafe.As<sbyte, TValue>(ref sbyteNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int16:
                case CoreEnumType.Int16Nullable:
                    if (Utf8Parser.TryParse(bytes, out short shortNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, shortNumber) : Unsafe.As<short, TValue>(ref shortNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt16:
                case CoreEnumType.UInt16Nullable:
                    if (Utf8Parser.TryParse(bytes, out ushort ushortNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, ushortNumber) : Unsafe.As<ushort, TValue>(ref ushortNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int32:
                case CoreEnumType.Int32Nullable:
                    if (Utf8Parser.TryParse(bytes, out int intNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, intNumber) : Unsafe.As<int, TValue>(ref intNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt32:
                case CoreEnumType.UInt32Nullable:
                    if (Utf8Parser.TryParse(bytes, out uint uintNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, uintNumber) : Unsafe.As<uint, TValue>(ref uintNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int64:
                case CoreEnumType.Int64Nullable:
                    if (Utf8Parser.TryParse(bytes, out long longNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, longNumber) : Unsafe.As<long, TValue>(ref longNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt64:
                case CoreEnumType.UInt64Nullable:
                    if (Utf8Parser.TryParse(bytes, out ulong ulongNumber, out consumed) && consumed == bytes.Length)
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, ulongNumber) : Unsafe.As<ulong, TValue>(ref ulongNumber);
                        return true;
                    }
                    break;
            }
            value = default;
            return false;
        }

        private bool TryParseNumber(ReadOnlySpan<char> chars, out TValue? value)
        {
#if NETSTANDARD2_0
            var text = chars.ToString();
#else
            var text = chars;
#endif
            switch (TypeDetail.EnumUnderlyingType!.Value)
            {
                case CoreEnumType.Byte:
                case CoreEnumType.ByteNullable:
                    if (Byte.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var byteNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, byteNumber) : Unsafe.As<byte, TValue>(ref byteNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.SByte:
                case CoreEnumType.SByteNullable:
                    if (SByte.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var sbyteNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, sbyteNumber) : Unsafe.As<sbyte, TValue>(ref sbyteNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int16:
                case CoreEnumType.Int16Nullable:
                    if (Int16.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var shortNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, shortNumber) : Unsafe.As<short, TValue>(ref shortNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt16:
                case CoreEnumType.UInt16Nullable:
                    if (UInt16.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var ushortNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, ushortNumber) : Unsafe.As<ushort, TValue>(ref ushortNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int32:
                case CoreEnumType.Int32Nullable:
                    if (Int32.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var intNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, intNumber) : Unsafe.As<int, TValue>(ref intNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt32:
                case CoreEnumType.UInt32Nullable:
                    if (UInt32.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var uintNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, uintNumber) : Unsafe.As<uint, TValue>(ref uintNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.Int64:
                case CoreEnumType.Int64Nullable:
                    if (Int64.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var longNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, longNumber) : Unsafe.As<long, TValue>(ref longNumber);
                        return true;
                    }
                    break;
                case CoreEnumType.UInt64:
                case CoreEnumType.UInt64Nullable:
                    if (UInt64.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var ulongNumber))
                    {
                        value = TypeDetail.IsNullable ? (TValue?)Enum.ToObject(TypeDetail.InnerType!, ulongNumber) : Unsafe.As<ulong, TValue>(ref ulongNumber);
                        return true;
                    }
                    break;
            }
            value = default;
            return false;
        }
    }
}