// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed class ByteConverterEnum<TValue> : ByteConverter<TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            switch (TypeDetail.EnumUnderlyingType!.Value)
            {
                case CoreEnumType.Byte:
                    {
                        if (!reader.TryRead(out byte number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.SByte:
                    {
                        if (!reader.TryRead(out sbyte number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int16:
                    {
                        if (!reader.TryRead(out short number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt16:
                    {
                        if (!reader.TryRead(out ushort number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int32:
                    {
                        if (!reader.TryRead(out int number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt32:
                    {
                        if (!reader.TryRead(out uint number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int64:
                    {
                        if (!reader.TryRead(out long number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt64:
                    {
                        if (!reader.TryRead(out ulong number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.ByteNullable:
                    {
                        if (!reader.TryRead(out byte? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.SByteNullable:
                    {
                        if (!reader.TryRead(out sbyte? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int16Nullable:
                    {
                        if (!reader.TryRead(out short? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt16Nullable:
                    {
                        if (!reader.TryRead(out ushort? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int32Nullable:
                    {
                        if (!reader.TryRead(out int? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt32Nullable:
                    {
                        if (!reader.TryRead(out uint? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.Int64Nullable:
                    {
                        if (!reader.TryRead(out long? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                case CoreEnumType.UInt64Nullable:
                    {
                        if (!reader.TryRead(out ulong? number, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number is null)
                        {
                            value = default;
                            return true;
                        }
                        if (!TypeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(TypeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(TypeDetail.InnerType!, number);
                        return true;
                    }
                default: throw new NotImplementedException();
            };
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value)
        {
            object obj = value!;

            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (TypeDetail.EnumUnderlyingType)
            {
                case CoreEnumType.Byte:
                    if (!writer.TryWrite((byte)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.SByte:
                    if (!writer.TryWrite((sbyte)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int16:
                    if (!writer.TryWrite((short)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt16:
                    if (!writer.TryWrite((ushort)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int32:
                    if (!writer.TryWrite((int)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt32:
                    if (!writer.TryWrite((uint)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int64:
                    if (!writer.TryWrite((long)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt64:
                    if (!writer.TryWrite((ulong)obj, out state.SizeNeeded))
                        return false;
                    return true;

                case CoreEnumType.ByteNullable:
                    if (!writer.TryWrite((byte)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.SByteNullable:
                    if (!writer.TryWrite((sbyte)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int16Nullable:
                    if (!writer.TryWrite((short)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt16Nullable:
                    if (!writer.TryWrite((ushort)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int32Nullable:
                    if (!writer.TryWrite((int)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt32Nullable:
                    if (!writer.TryWrite((uint)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int64Nullable:
                    if (!writer.TryWrite((long)obj, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt64Nullable:
                    if (!writer.TryWrite((ulong)obj, out state.SizeNeeded))
                        return false;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}