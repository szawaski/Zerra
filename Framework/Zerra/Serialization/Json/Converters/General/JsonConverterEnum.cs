// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers.Text;
using System.Globalization;
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
                    //names already seen are found without allocating a string
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
                    else
                    {
                        if (state.ErrorOnTypeMismatch)
                            ThrowCannotConvert(ref reader);
                        value = default;
                    }
                    return true;
                case JsonToken.Number:
                    {
                        //enum values can be negative or above long.MaxValue for ulong enums
                        object? number = null;
                        if (reader.UseBytes)
                        {
                            if (Utf8Parser.TryParse(reader.ValueBytes, out long signed, out var consumed) && consumed == reader.ValueBytes.Length)
                                number = signed;
                            else if (Utf8Parser.TryParse(reader.ValueBytes, out ulong unsigned, out consumed) && consumed == reader.ValueBytes.Length)
                                number = unsigned;
                        }
                        else
                        {
#if NETSTANDARD2_0
                            var chars = reader.ValueChars.ToString();
#else
                            var chars = reader.ValueChars;
#endif
                            if (Int64.TryParse(chars, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var signed))
                                number = signed;
                            else if (UInt64.TryParse(chars, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var unsigned))
                                number = unsigned;
                        }
                        if (number is null)
                        {
                            if (state.ErrorOnTypeMismatch)
                                ThrowCannotConvert(ref reader);
                            number = 0L;
                        }
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
    }
}