﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        public byte[] Serialize(object item)
        {
            if (item == null)
                return null;

            var type = item.GetType();

            return Serialize(item, type);
        }
        public byte[] Serialize<T>(T item)
        {
            if (item == null)
                return null;

            return Serialize(item, typeof(T));
        }
        public byte[] Serialize(object obj, Type type)
        {
            if (obj == null)
                return null;

            var typeDetail = GetTypeInformation(type, this.indexSize, this.ignoreIndexAttribute);

            var writer = new ByteWriter(defaultBufferSize, encoding);
            try
            {
                ToBytes(obj, typeDetail, true, ref writer);
                var result = writer.ToArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }

        private void ToBytes(object value, SerializerTypeDetails typeDetail, bool nullFlags, ref ByteWriter writer)
        {
            if (includePropertyTypes)
            {
                var typeFromValue = value.GetType();
                var typeName = typeFromValue.FullName;
                writer.Write(typeName, false);
                typeDetail = GetTypeInformation(typeFromValue, this.indexSize, this.ignoreIndexAttribute);
            }
            else if (typeDetail.Type.IsInterface && !typeDetail.TypeDetail.IsIEnumerableGeneric && value != null)
            {
                var objectType = value.GetType();
                typeDetail = GetTypeInformation(objectType, this.indexSize, this.ignoreIndexAttribute);
            }
            else if (typeDetail == null)
            {
                throw new InvalidOperationException("Must include type information to deserialize without specifying a type.");
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                ToBytesCoreType(value, typeDetail.TypeDetail.CoreType.Value, nullFlags, ref writer);
                return;
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                ToBytesEnumType(value, typeDetail.TypeDetail.EnumUnderlyingType.Value, nullFlags, ref writer);
                return;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                ToBytesSpecialType(value, typeDetail, nullFlags, ref writer);
                return;
            }

            if (typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                if (typeDetail.TypeDetail.IsICollection)
                {
                    var collection = (ICollection)value;
                    var count = collection.Count;
                    ToBytesEnumerable(collection, count, typeDetail.InnerTypeDetail, ref writer);
                }
                else if (typeDetail.TypeDetail.IsICollectionGeneric)
                {
                    var count = (int)typeDetail.TypeDetail.GetMember("Count").Getter(value);
                    ToBytesEnumerable((IEnumerable)value, count, typeDetail.InnerTypeDetail, ref writer);
                }
                else
                {
                    var enumerable = (IEnumerable)value;
                    var count = 0;
                    foreach (var item in enumerable)
                        count++;
                    ToBytesEnumerable(enumerable, count, typeDetail.InnerTypeDetail, ref writer);
                }
                return;
            }

            if (nullFlags)
            {
                if (value == null)
                {
                    writer.Write(false); //no object
                    return;
                }
                writer.Write(true); //has object
            }
            foreach (var indexProperty in typeDetail.IndexedProperties)
            {
                var propertyValue = indexProperty.Value.Getter(value);
                if (propertyValue != null)
                {
                    if (usePropertyNames)
                    {
                        writer.Write(indexProperty.Value.Name, false);
                    }
                    else
                    {
                        switch (this.indexSize)
                        {
                            case ByteSerializerIndexSize.Byte:
                                writer.Write((byte)indexProperty.Key);
                                break;
                            case ByteSerializerIndexSize.UInt16:
                                writer.Write(indexProperty.Key);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    ToBytes(propertyValue, indexProperty.Value.SerailzierTypeDetails, false, ref writer);
                }
            }

            if (usePropertyNames)
            {
                writer.Write(0);
            }
            else
            {
                switch (this.indexSize)
                {
                    case ByteSerializerIndexSize.Byte:
                        writer.Write(endObjectFlagByte);
                        break;
                    case ByteSerializerIndexSize.UInt16:
                        writer.Write(endObjectFlagUInt16);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ToBytesEnumerable(IEnumerable values, int length, SerializerTypeDetails typeDetail, ref ByteWriter writer)
        {
            writer.Write(length); //object count

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                ToBytesCoreTypeEnumerable(values, length, typeDetail.TypeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                ToBytesEnumTypeEnumerable(values, length, typeDetail.TypeDetail.EnumUnderlyingType.Value, ref writer);
                return;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                ToBytesSpecialTypeEnumerable(values, length, typeDetail, ref writer);
                return;
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    writer.Write(false); //no object
                    continue;
                }
                writer.Write(true); //has object

                ToBytes(value, typeDetail, false, ref writer);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesCoreType(object value, CoreType coreType, bool nullFlags, ref ByteWriter writer)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (coreType)
            {
                case CoreType.Boolean:
                    writer.Write((bool)value);
                    return;
                case CoreType.Byte:
                    writer.Write((byte)value);
                    return;
                case CoreType.SByte:
                    writer.Write((sbyte)value);
                    return;
                case CoreType.Int16:
                    writer.Write((short)value);
                    return;
                case CoreType.UInt16:
                    writer.Write((ushort)value);
                    return;
                case CoreType.Int32:
                    writer.Write((int)value);
                    return;
                case CoreType.UInt32:
                    writer.Write((uint)value);
                    return;
                case CoreType.Int64:
                    writer.Write((long)value);
                    return;
                case CoreType.UInt64:
                    writer.Write((ulong)value);
                    return;
                case CoreType.Single:
                    writer.Write((float)value);
                    return;
                case CoreType.Double:
                    writer.Write((double)value);
                    return;
                case CoreType.Decimal:
                    writer.Write((decimal)value);
                    return;
                case CoreType.Char:
                    writer.Write((char)value);
                    return;
                case CoreType.DateTime:
                    writer.Write((DateTime)value);
                    return;
                case CoreType.DateTimeOffset:
                    writer.Write((DateTimeOffset)value);
                    return;
                case CoreType.TimeSpan:
                    writer.Write((TimeSpan)value);
                    return;
                case CoreType.Guid:
                    writer.Write((Guid)value);
                    return;

                case CoreType.String:
                    writer.Write((string)value, nullFlags);
                    return;

                case CoreType.BooleanNullable:
                    writer.Write((bool?)value, nullFlags);
                    return;
                case CoreType.ByteNullable:
                    writer.Write((byte?)value, nullFlags);
                    return;
                case CoreType.SByteNullable:
                    writer.Write((sbyte?)value, nullFlags);
                    return;
                case CoreType.Int16Nullable:
                    writer.Write((short?)value, nullFlags);
                    return;
                case CoreType.UInt16Nullable:
                    writer.Write((ushort?)value, nullFlags);
                    return;
                case CoreType.Int32Nullable:
                    writer.Write((int?)value, nullFlags);
                    return;
                case CoreType.UInt32Nullable:
                    writer.Write((uint?)value, nullFlags);
                    return;
                case CoreType.Int64Nullable:
                    writer.Write((long?)value, nullFlags);
                    return;
                case CoreType.UInt64Nullable:
                    writer.Write((ulong?)value, nullFlags);
                    return;
                case CoreType.SingleNullable:
                    writer.Write((float?)value, nullFlags);
                    return;
                case CoreType.DoubleNullable:
                    writer.Write((double?)value, nullFlags);
                    return;
                case CoreType.DecimalNullable:
                    writer.Write((decimal?)value, nullFlags);
                    return;
                case CoreType.CharNullable:
                    writer.Write((char?)value, nullFlags);
                    return;
                case CoreType.DateTimeNullable:
                    writer.Write((DateTime?)value, nullFlags);
                    return;
                case CoreType.DateTimeOffsetNullable:
                    writer.Write((DateTimeOffset?)value, nullFlags);
                    return;
                case CoreType.TimeSpanNullable:
                    writer.Write((TimeSpan?)value, nullFlags);
                    return;
                case CoreType.GuidNullable:
                    writer.Write((Guid?)value, nullFlags);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesCoreTypeEnumerable(IEnumerable values, int length, CoreType coreType, ref ByteWriter writer)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (coreType)
            {
                case CoreType.Boolean:
                    writer.Write((IEnumerable<bool>)values, length);
                    return;
                case CoreType.Byte:
                    writer.Write((IEnumerable<byte>)values, length);
                    return;
                case CoreType.SByte:
                    writer.Write((IEnumerable<sbyte>)values, length);
                    return;
                case CoreType.Int16:
                    writer.Write((IEnumerable<short>)values, length);
                    return;
                case CoreType.UInt16:
                    writer.Write((IEnumerable<ushort>)values, length);
                    return;
                case CoreType.Int32:
                    writer.Write((IEnumerable<int>)values, length);
                    return;
                case CoreType.UInt32:
                    writer.Write((IEnumerable<uint>)values, length);
                    return;
                case CoreType.Int64:
                    writer.Write((IEnumerable<long>)values, length);
                    return;
                case CoreType.UInt64:
                    writer.Write((IEnumerable<ulong>)values, length);
                    return;
                case CoreType.Single:
                    writer.Write((IEnumerable<float>)values, length);
                    return;
                case CoreType.Double:
                    writer.Write((IEnumerable<double>)values, length);
                    return;
                case CoreType.Decimal:
                    writer.Write((IEnumerable<decimal>)values, length);
                    return;
                case CoreType.Char:
                    writer.Write((IEnumerable<char>)values, length);
                    return;
                case CoreType.DateTime:
                    writer.Write((IEnumerable<DateTime>)values, length);
                    return;
                case CoreType.DateTimeOffset:
                    writer.Write((IEnumerable<DateTimeOffset>)values, length);
                    return;
                case CoreType.TimeSpan:
                    writer.Write((IEnumerable<TimeSpan>)values, length);
                    return;
                case CoreType.Guid:
                    writer.Write((IEnumerable<Guid>)values, length);
                    return;

                case CoreType.String:
                    writer.Write((IEnumerable<string>)values, length);
                    return;

                case CoreType.BooleanNullable:
                    writer.Write((IEnumerable<bool?>)values, length);
                    return;
                case CoreType.ByteNullable:
                    writer.Write((IEnumerable<byte?>)values, length);
                    return;
                case CoreType.SByteNullable:
                    writer.Write((IEnumerable<sbyte?>)values, length);
                    return;
                case CoreType.Int16Nullable:
                    writer.Write((IEnumerable<short?>)values, length);
                    return;
                case CoreType.UInt16Nullable:
                    writer.Write((IEnumerable<ushort?>)values, length);
                    return;
                case CoreType.Int32Nullable:
                    writer.Write((IEnumerable<int?>)values, length);
                    return;
                case CoreType.UInt32Nullable:
                    writer.Write((IEnumerable<uint?>)values, length);
                    return;
                case CoreType.Int64Nullable:
                    writer.Write((IEnumerable<long?>)values, length);
                    return;
                case CoreType.UInt64Nullable:
                    writer.Write((IEnumerable<ulong?>)values, length);
                    return;
                case CoreType.SingleNullable:
                    writer.Write((IEnumerable<float?>)values, length);
                    return;
                case CoreType.DoubleNullable:
                    writer.Write((IEnumerable<double?>)values, length);
                    return;
                case CoreType.DecimalNullable:
                    writer.Write((IEnumerable<decimal?>)values, length);
                    return;
                case CoreType.CharNullable:
                    writer.Write((IEnumerable<char?>)values, length);
                    return;
                case CoreType.DateTimeNullable:
                    writer.Write((IEnumerable<DateTime?>)values, length);
                    return;
                case CoreType.DateTimeOffsetNullable:
                    writer.Write((IEnumerable<DateTimeOffset?>)values, length);
                    return;
                case CoreType.TimeSpanNullable:
                    writer.Write((IEnumerable<TimeSpan?>)values, length);
                    return;
                case CoreType.GuidNullable:
                    writer.Write((IEnumerable<Guid?>)values, length);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesEnumType(object value, CoreType coreType, bool nullFlags, ref ByteWriter writer)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (coreType)
            {
                case CoreType.Byte:
                    writer.Write((byte)value);
                    return;
                case CoreType.SByte:
                    writer.Write((sbyte)value);
                    return;
                case CoreType.Int16:
                    writer.Write((short)value);
                    return;
                case CoreType.UInt16:
                    writer.Write((ushort)value);
                    return;
                case CoreType.Int32:
                    writer.Write((int)value);
                    return;
                case CoreType.UInt32:
                    writer.Write((uint)value);
                    return;
                case CoreType.Int64:
                    writer.Write((long)value);
                    return;
                case CoreType.UInt64:
                    writer.Write((ulong)value);
                    return;

                case CoreType.ByteNullable:
                    writer.Write(value == null ? null : (byte?)(byte)value, nullFlags);
                    return;
                case CoreType.SByteNullable:
                    writer.Write(value == null ? null : (sbyte?)(sbyte)value, nullFlags);
                    return;
                case CoreType.Int16Nullable:
                    writer.Write(value == null ? null : (short?)(short)value, nullFlags);
                    return;
                case CoreType.UInt16Nullable:
                    writer.Write(value == null ? null : (ushort?)(ushort)value, nullFlags);
                    return;
                case CoreType.Int32Nullable:
                    writer.Write(value == null ? null : (int?)(int)value, nullFlags);
                    return;
                case CoreType.UInt32Nullable:
                    writer.Write(value == null ? null : (uint?)(uint)value, nullFlags);
                    return;
                case CoreType.Int64Nullable:
                    writer.Write(value == null ? null : (long?)(long)value, nullFlags);
                    return;
                case CoreType.UInt64Nullable:
                    writer.Write(value == null ? null : (ulong?)(ulong)value, nullFlags);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesEnumTypeEnumerable(IEnumerable values, int length, CoreType coreType, ref ByteWriter writer)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless nullFlags = true
            switch (coreType)
            {
                case CoreType.Byte:
                    writer.WriteByteCast(values, length);
                    return;
                case CoreType.SByte:
                    writer.WriteSByteCast(values, length);
                    return;
                case CoreType.Int16:
                    writer.WriteInt16Cast(values, length);
                    return;
                case CoreType.UInt16:
                    writer.WriteUInt16Cast(values, length);
                    return;
                case CoreType.Int32:
                    writer.WriteInt32Cast(values, length);
                    return;
                case CoreType.UInt32:
                    writer.WriteUInt32Cast(values, length);
                    return;
                case CoreType.Int64:
                    writer.WriteInt64Cast(values, length);
                    return;
                case CoreType.UInt64:
                    writer.WriteUInt64Cast(values, length);
                    return;

                case CoreType.ByteNullable:
                    writer.WriteByteNullableCast(values, length);
                    return;
                case CoreType.SByteNullable:
                    writer.WriteSByteNullableCast(values, length);
                    return;
                case CoreType.Int16Nullable:
                    writer.WriteInt16NullableCast(values, length);
                    return;
                case CoreType.UInt16Nullable:
                    writer.WriteUInt16NullableCast(values, length);
                    return;
                case CoreType.Int32Nullable:
                    writer.WriteInt32NullableCast(values, length);
                    return;
                case CoreType.UInt32Nullable:
                    writer.WriteUInt32NullableCast(values, length);
                    return;
                case CoreType.Int64Nullable:
                    writer.WriteUInt64NullableCast(values, length);
                    return;
                case CoreType.UInt64Nullable:
                    writer.WriteUInt64NullableCast(values, length);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesSpecialType(object value, SerializerTypeDetails typeDetail, bool nullFlags, ref ByteWriter writer)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var valueType = value == null ? null : (Type)value;
                        writer.Write(valueType?.FullName, nullFlags);
                    }
                    return;
                case SpecialType.Dictionary:
                    {
                        if (value != null)
                        {
                            if (nullFlags)
                                writer.WriteNotNull();
                            var method = TypeAnalyzer.GetGenericMethodDetail(enumerableToArrayMethod, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                            var innerValue = (ICollection)method.Caller(null, new object[] { value });
                            var count = innerValue.Count;
                            ToBytesEnumerable(innerValue, count, typeDetail.InnerTypeDetail, ref writer);
                        }
                        else if (nullFlags)
                        {
                            writer.WriteNull();
                        }
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToBytesSpecialTypeEnumerable(IEnumerable values, int length, SerializerTypeDetails typeDetail, ref ByteWriter writer)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        foreach (var valueType in (IEnumerable<Type>)values)
                        {
                            writer.Write(valueType?.FullName, true);
                        }
                        return;
                    }
                case SpecialType.Dictionary:
                    {
                        foreach (var value in values)
                        {
                            if (value != null)
                            {
                                writer.WriteNotNull();
                                var method = TypeAnalyzer.GetGenericMethodDetail(enumerableToArrayMethod, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                                var innerValue = (ICollection)method.Caller(null, new object[] { value });
                                var count = innerValue.Count;
                                ToBytesEnumerable(innerValue, count, typeDetail.InnerTypeDetail, ref writer);
                            }
                            else
                            {
                                writer.WriteNull();
                            }
                        }
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}