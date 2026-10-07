// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
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
                            value = Unsafe.As<byte, TValue>(ref number);
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
                            value = Unsafe.As<sbyte, TValue>(ref number);
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
                            value = Unsafe.As<short, TValue>(ref number);
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
                            value = Unsafe.As<ushort, TValue>(ref number);
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
                            value = Unsafe.As<int, TValue>(ref number);
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
                            value = Unsafe.As<uint, TValue>(ref number);
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
                            value = Unsafe.As<long, TValue>(ref number);
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
                            value = Unsafe.As<ulong, TValue>(ref number);
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
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (TypeDetail.EnumUnderlyingType)
            {
                case CoreEnumType.Byte:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (byte)(object)value! : Unsafe.As<TValue, byte>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.SByte:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (sbyte)(object)value! : Unsafe.As<TValue, sbyte>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int16:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (short)(object)value! : Unsafe.As<TValue, short>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt16:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (ushort)(object)value! : Unsafe.As<TValue, ushort>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int32:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (int)(object)value! : Unsafe.As<TValue, int>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt32:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (uint)(object)value! : Unsafe.As<TValue, uint>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int64:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (long)(object)value! : Unsafe.As<TValue, long>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt64:
                    if (!writer.TryWrite(TypeDetail.IsNullable ? (ulong)(object)value! : Unsafe.As<TValue, ulong>(ref Unsafe.AsRef(in value)), out state.SizeNeeded))
                        return false;
                    return true;

                case CoreEnumType.ByteNullable:
                    if (!writer.TryWrite((byte)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.SByteNullable:
                    if (!writer.TryWrite((sbyte)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int16Nullable:
                    if (!writer.TryWrite((short)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt16Nullable:
                    if (!writer.TryWrite((ushort)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int32Nullable:
                    if (!writer.TryWrite((int)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt32Nullable:
                    if (!writer.TryWrite((uint)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.Int64Nullable:
                    if (!writer.TryWrite((long)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                case CoreEnumType.UInt64Nullable:
                    if (!writer.TryWrite((ulong)(object)value!, out state.SizeNeeded))
                        return false;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}