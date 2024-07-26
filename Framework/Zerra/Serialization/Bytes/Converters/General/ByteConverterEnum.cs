﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed class ByteConverterEnum<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            switch (typeDetail.EnumUnderlyingType!.Value)
            {
                case CoreType.Byte:
                    {
                        if (!reader.TryRead(out byte number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out sbyte number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out short number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out ushort number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out int number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out uint number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out long number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out ulong number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out byte? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out sbyte? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out short? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out ushort? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out int? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out uint? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out long? number, out state.BytesNeeded))
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
                        if (!reader.TryRead(out ulong? number, out state.BytesNeeded))
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value)
        {
            object obj = value!;

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
                    if (!writer.TryWrite((byte)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.SByteNullable:
                    if (!writer.TryWrite((sbyte)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite((short)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite((ushort)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite((int)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite((uint)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite((long)obj, out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite((ulong)obj, out state.BytesNeeded))
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