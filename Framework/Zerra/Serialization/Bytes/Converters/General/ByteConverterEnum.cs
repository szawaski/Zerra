// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterEnum<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            switch (typeDetail.EnumUnderlyingType!.Value)
            {
                case CoreType.Byte:
                    {
                        if (!reader.TryReadByte(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.SByte:
                    {
                        if (!reader.TryReadSByte(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int16:
                    {
                        if (!reader.TryReadInt16(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt16:
                    {
                        if (!reader.TryReadUInt16(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int32:
                    {
                        if (!reader.TryReadInt32(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt32:
                    {
                        if (!reader.TryReadUInt32(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int64:
                    {
                        if (!reader.TryReadInt64(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt64:
                    {
                        if (!reader.TryReadUInt64(out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.ByteNullable:
                    {
                        if (!reader.TryReadByteNullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.SByteNullable:
                    {
                        if (!reader.TryReadSByteNullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int16Nullable:
                    {
                        if (!reader.TryReadInt16Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt16Nullable:
                    {
                        if (!reader.TryReadUInt16Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int32Nullable:
                    {
                        if (!reader.TryReadInt32Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt32Nullable:
                    {
                        if (!reader.TryReadUInt32Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.Int64Nullable:
                    {
                        if (!reader.TryReadInt64Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                case CoreType.UInt64Nullable:
                    {
                        if (!reader.TryReadUInt64Nullable(state.Current.NullFlags, out var number, out state.BytesNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if (number == null)
                        {
                            value = default;
                            return true;
                        }
                        if (!typeDetail.IsNullable)
                            value = (TValue)Enum.ToObject(typeDetail.Type, number);
                        else
                            value = (TValue)Enum.ToObject(typeDetail.InnerTypes[0], number);
                        return true;
                    }
                default: throw new NotImplementedException();
            };
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue? value)
        {
            object? obj = value;

            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (typeDetail.EnumUnderlyingType)
            {
                case CoreType.Byte:
                    if (!writer.TryWrite((byte)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.SByte:
                    if (!writer.TryWrite((sbyte)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int16:
                    if (!writer.TryWrite((short)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt16:
                    if (!writer.TryWrite((ushort)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int32:
                    if (!writer.TryWrite((int)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt32:
                    if (!writer.TryWrite((uint)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int64:
                    if (!writer.TryWrite((long)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt64:
                    if (!writer.TryWrite((ulong)obj!, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;

                case CoreType.ByteNullable:
                    if (!writer.TryWrite(obj == null ? null : (byte?)(byte)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.SByteNullable:
                    if (!writer.TryWrite(obj == null ? null : (sbyte?)(sbyte)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite(obj == null ? null : (short?)(short)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite(obj == null ? null : (ushort?)(ushort)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite(obj == null ? null : (int?)(int)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite(obj == null ? null : (uint?)(uint)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite(obj == null ? null : (long?)(long)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite(obj == null ? null : (ulong?)(ulong)obj, state.Current.NullFlags, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}