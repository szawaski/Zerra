// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        public T Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return default(T);

            return (T)Deserialize(typeof(T), bytes);
        }
        public object Deserialize(Type type, ReadOnlySpan<byte> bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var typeDetail = GetTypeInformation(type, this.indexSize, this.ignoreIndexAttribute);

            var reader = new ByteReader(bytes, encoding);
            var obj = FromBytes(ref reader, typeDetail, true, false);
            return obj;
        }

        private object FromBytes(ref ByteReader reader, SerializerTypeDetails typeDetail, bool nullFlags, bool drainBytes)
        {
            if (!drainBytes && typeDetail == null)
                throw new NotSupportedException("Cannot deserialize without type information");
            if (includePropertyTypes)
            {
                string typeName = reader.ReadString(false);
                var typeFromBytes = Discovery.GetTypeFromName(typeName);
                //overrides potentially boxed type with actual type if exists in assembly
                if (typeFromBytes != null)
                {
                    var newTypeDetail = GetTypeInformation(typeFromBytes, this.indexSize, this.ignoreIndexAttribute);

                    var typeDetailCheck = typeDetail.TypeDetail;
                    if (typeDetailCheck.IsNullable)
                        typeDetailCheck = typeDetailCheck.InnerTypeDetails[0];
                    var newTypeDetailCheck = newTypeDetail.TypeDetail;

                    if (newTypeDetailCheck.Type != typeDetailCheck.Type && !newTypeDetailCheck.Interfaces.Contains(typeDetailCheck.Type) && !newTypeDetail.TypeDetail.BaseTypes.Contains(typeDetailCheck.Type))
                        throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.TypeDetail.Type.GetNiceName()}");

                    typeDetail = newTypeDetail;
                }
            }
            else if (typeDetail.Type.IsInterface && !typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = GetTypeInformation(emptyImplementationType, this.indexSize, this.ignoreIndexAttribute);
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                return FromBytesCoreType(ref reader, typeDetail.TypeDetail.CoreType.Value, nullFlags);
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                var numValue = FromBytesCoreType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, nullFlags);
                object value;
                if (!typeDetail.TypeDetail.IsNullable)
                    value = Enum.ToObject(typeDetail.Type, numValue);
                else if (numValue != null)
                    value = Enum.ToObject(typeDetail.TypeDetail.InnerTypes[0], numValue);
                else
                    value = null;
                return value;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                return FromBytesSpecialType(ref reader, typeDetail, nullFlags);
            }

            if (typeDetail.TypeDetail.IsIEnumerableGeneric)
            {
                var enumerable = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.IsIList);
                return enumerable;
            }

            if (nullFlags)
            {
                var hasObject = reader.ReadBoolean();

                if (!hasObject)
                {
                    return null;
                }
            }

            object obj = typeDetail.TypeDetail.Creator();

            for (; ; )
            {
                SerializerMemberDetails indexProperty = null;

                if (usePropertyNames)
                {
                    string name = reader.ReadString(false);

                    if (name == String.Empty)
                        return obj;

                    indexProperty = typeDetail.IndexedProperties.Values.FirstOrDefault(x => x.Name == name);

                    if (indexProperty == null)
                    {
                        if (!usePropertyNames && !includePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        object value = FromBytes(ref reader, null, false, true);
                        indexProperty.Setter(obj, value);
                    }
                    else
                    {
                        object value = FromBytes(ref reader, indexProperty.SerailzierTypeDetails, false, false);
                        indexProperty.Setter(obj, value);
                    }
                }
                else
                {
                    var propertyIndex = this.indexSize switch
                    {
                        ByteSerializerIndexSize.Byte => (ushort)reader.ReadByte(),
                        ByteSerializerIndexSize.UInt16 => reader.ReadUInt16(),
                        _ => throw new NotImplementedException(),
                    };

                    if (propertyIndex == endObjectFlagUShort)
                        return obj;

                    if (typeDetail.IndexedProperties.Keys.Contains(propertyIndex))
                        indexProperty = typeDetail.IndexedProperties[propertyIndex];

                    if (indexProperty == null)
                    {
                        if (!usePropertyNames && !includePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        object value = FromBytes(ref reader, null, false, true);
                        indexProperty.Setter(obj, value);
                    }
                    else
                    {
                        object value = FromBytes(ref reader, indexProperty.SerailzierTypeDetails, false, false);
                        indexProperty.Setter(obj, value);
                    }
                }
            }

            throw new Exception("Expected end of object marker");
        }
        private object FromBytesEnumerable(ref ByteReader reader, SerializerTypeDetails typeDetail, bool asList)
        {
            int length = reader.ReadInt32();

            if (length == 0)
            {
                if (!asList)
                    return Array.CreateInstance(typeDetail.Type, length);
                else
                    return (IList)typeDetail.ListCreator(length);
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                return FromBytesCoreTypeEnumerable(ref reader, length, typeDetail.TypeDetail.CoreType.Value, asList);
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                if (!asList)
                {
                    var array = Array.CreateInstance(typeDetail.Type, length);
                    for (int i = 0; i < length; i++)
                    {
                        var numValue = FromBytesCoreType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, true);
                        object item;
                        if (!typeDetail.TypeDetail.IsNullable)
                            item = Enum.ToObject(typeDetail.Type, numValue);
                        else if (numValue != null)
                            item = Enum.ToObject(typeDetail.TypeDetail.InnerTypes[0], numValue);
                        else
                            item = null;
                        array.SetValue(item, i);
                    }
                    return array;
                }
                else
                {
                    var list = (IList)typeDetail.ListCreator(length);
                    for (int i = 0; i < length; i++)
                    {
                        var numValue = FromBytesCoreType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, true);
                        object item;
                        if (!typeDetail.TypeDetail.IsNullable)
                            item = Enum.ToObject(typeDetail.Type, numValue);
                        else if (numValue != null)
                            item = Enum.ToObject(typeDetail.TypeDetail.InnerTypes[0], numValue);
                        else
                            item = null;
                        list.Add(item);
                    }
                    return list;
                }
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                return FromBytesSpecialTypeEnumerable(ref reader, length, typeDetail, asList);
            }

            object obj = null;

            if (!asList)
            {
                var array = Array.CreateInstance(typeDetail.Type, length);
                if (length == 0)
                    return array;

                int count = 0;
                for (; ; )
                {
                    var hasObject = reader.ReadBoolean();
                    if (!hasObject)
                    {
                        count++;
                        if (count == length)
                            return array;
                        continue;
                    }

                    obj = FromBytes(ref reader, typeDetail, false, false);
                    array.SetValue(obj, count);
                    count++;
                    if (count == length)
                        return array;
                }

                throw new Exception("Expected end of object marker");
            }
            else
            {
                var list = (IList)typeDetail.ListCreator(length);
                if (length == 0)
                    return list;

                int count = 0;
                for (; ; )
                {
                    var hasObject = reader.ReadBoolean();
                    if (!hasObject)
                    {
                        list.Add(null);
                        count++;
                        if (count == length)
                            return list;
                        continue;
                    }

                    obj = FromBytes(ref reader, typeDetail, false, false);
                    list.Add(obj);
                    count++;
                    if (count == length)
                        return list;
                }

                throw new Exception("Expected end of object marker");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object FromBytesCoreType(ref ByteReader reader, CoreType coreType, bool nullFlags)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless coreTypeCouldBeNull = true
            return coreType switch
            {
                CoreType.Boolean => reader.ReadBoolean(),
                CoreType.Byte => reader.ReadByte(),
                CoreType.SByte => reader.ReadSByte(),
                CoreType.Int16 => reader.ReadInt16(),
                CoreType.UInt16 => reader.ReadUInt16(),
                CoreType.Int32 => reader.ReadInt32(),
                CoreType.UInt32 => reader.ReadUInt32(),
                CoreType.Int64 => reader.ReadInt64(),
                CoreType.UInt64 => reader.ReadUInt64(),
                CoreType.Single => reader.ReadSingle(),
                CoreType.Double => reader.ReadDouble(),
                CoreType.Decimal => reader.ReadDecimal(),
                CoreType.Char => reader.ReadChar(),
                CoreType.DateTime => reader.ReadDateTime(),
                CoreType.DateTimeOffset => reader.ReadDateTimeOffset(),
                CoreType.TimeSpan => reader.ReadTimeSpan(),
                CoreType.Guid => reader.ReadGuid(),
                CoreType.String => reader.ReadString(nullFlags),
                CoreType.BooleanNullable => reader.ReadBooleanNullable(nullFlags),
                CoreType.ByteNullable => reader.ReadByteNullable(nullFlags),
                CoreType.SByteNullable => reader.ReadSByteNullable(nullFlags),
                CoreType.Int16Nullable => reader.ReadInt16Nullable(nullFlags),
                CoreType.UInt16Nullable => reader.ReadUInt16Nullable(nullFlags),
                CoreType.Int32Nullable => reader.ReadInt32Nullable(nullFlags),
                CoreType.UInt32Nullable => reader.ReadUInt32Nullable(nullFlags),
                CoreType.Int64Nullable => reader.ReadInt64Nullable(nullFlags),
                CoreType.UInt64Nullable => reader.ReadUInt64Nullable(nullFlags),
                CoreType.SingleNullable => reader.ReadSingleNullable(nullFlags),
                CoreType.DoubleNullable => reader.ReadDoubleNullable(nullFlags),
                CoreType.DecimalNullable => reader.ReadDecimalNullable(nullFlags),
                CoreType.CharNullable => reader.ReadCharNullable(nullFlags),
                CoreType.DateTimeNullable => reader.ReadDateTimeNullable(nullFlags),
                CoreType.DateTimeOffsetNullable => reader.ReadDateTimeOffsetNullable(nullFlags),
                CoreType.TimeSpanNullable => reader.ReadTimeSpanNullable(nullFlags),
                CoreType.GuidNullable => reader.ReadGuidNullable(nullFlags),
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object FromBytesCoreTypeEnumerable(ref ByteReader reader, int length, CoreType coreType, bool asList)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless coreTypeCouldBeNull = true
            switch (coreType)
            {
                case CoreType.Boolean:
                    if (!asList)
                        return reader.ReadBooleanArray(length);
                    else
                        return reader.ReadBooleanList(length);
                case CoreType.Byte:
                    if (!asList)
                        return reader.ReadByteArray(length);
                    else
                        return reader.ReadByteList(length);
                case CoreType.SByte:
                    if (!asList)
                        return reader.ReadSByteArray(length);
                    else
                        return reader.ReadSByteList(length);
                case CoreType.Int16:
                    if (!asList)
                        return reader.ReadInt16Array(length);
                    else
                        return reader.ReadInt16List(length);
                case CoreType.UInt16:
                    if (!asList)
                        return reader.ReadUInt16Array(length);
                    else
                        return reader.ReadUInt16List(length);
                case CoreType.Int32:
                    if (!asList)
                        return reader.ReadInt32Array(length);
                    else
                        return reader.ReadInt32List(length);
                case CoreType.UInt32:
                    if (!asList)
                        return reader.ReadUInt32Array(length);
                    else
                        return reader.ReadUInt32List(length);
                case CoreType.Int64:
                    if (!asList)
                        return reader.ReadInt64Array(length);
                    else
                        return reader.ReadInt64List(length);
                case CoreType.UInt64:
                    if (!asList)
                        return reader.ReadUInt64Array(length);
                    else
                        return reader.ReadUInt64List(length);
                case CoreType.Single:
                    if (!asList)
                        return reader.ReadSingleArray(length);
                    else
                        return reader.ReadSingleList(length);
                case CoreType.Double:
                    if (!asList)
                        return reader.ReadDoubleArray(length);
                    else
                        return reader.ReadDoubleList(length);
                case CoreType.Decimal:
                    if (!asList)
                        return reader.ReadDecimalArray(length);
                    else
                        return reader.ReadDecimalList(length);
                case CoreType.Char:
                    if (!asList)
                        return reader.ReadCharArray(length);
                    else
                        return reader.ReadCharList(length);
                case CoreType.DateTime:
                    if (!asList)
                        return reader.ReadDateTimeArray(length);
                    else
                        return reader.ReadDateTimeList(length);
                case CoreType.DateTimeOffset:
                    if (!asList)
                        return reader.ReadDateTimeOffsetArray(length);
                    else
                        return reader.ReadDateTimeOffsetList(length);
                case CoreType.TimeSpan:
                    if (!asList)
                        return reader.ReadTimeSpanArray(length);
                    else
                        return reader.ReadTimeSpanList(length);
                case CoreType.Guid:
                    if (!asList)
                        return reader.ReadGuidArray(length);
                    else
                        return reader.ReadGuidList(length);

                case CoreType.String:
                    if (!asList)
                        return reader.ReadStringArray(length);
                    else
                        return reader.ReadStringList(length);

                case CoreType.BooleanNullable:
                    if (!asList)
                        return reader.ReadBooleanNullableArray(length);
                    else
                        return reader.ReadBooleanNullableList(length);
                case CoreType.ByteNullable:
                    if (!asList)
                        return reader.ReadByteNullableArray(length);
                    else
                        return reader.ReadByteNullableList(length);
                case CoreType.SByteNullable:
                    if (!asList)
                        return reader.ReadSByteNullableArray(length);
                    else
                        return reader.ReadSByteNullableList(length);
                case CoreType.Int16Nullable:
                    if (!asList)
                        return reader.ReadInt16NullableArray(length);
                    else
                        return reader.ReadInt16NullableList(length);
                case CoreType.UInt16Nullable:
                    if (!asList)
                        return reader.ReadUInt16NullableArray(length);
                    else
                        return reader.ReadUInt16NullableList(length);
                case CoreType.Int32Nullable:
                    if (!asList)
                        return reader.ReadInt32NullableArray(length);
                    else
                        return reader.ReadInt32NullableList(length);
                case CoreType.UInt32Nullable:
                    if (!asList)
                        return reader.ReadUInt32NullableArray(length);
                    else
                        return reader.ReadUInt32NullableList(length);
                case CoreType.Int64Nullable:
                    if (!asList)
                        return reader.ReadInt64NullableArray(length);
                    else
                        return reader.ReadInt64NullableList(length);
                case CoreType.UInt64Nullable:
                    if (!asList)
                        return reader.ReadUInt64NullableArray(length);
                    else
                        return reader.ReadUInt64NullableList(length);
                case CoreType.SingleNullable:
                    if (!asList)
                        return reader.ReadSingleNullableArray(length);
                    else
                        return reader.ReadSingleNullableList(length);
                case CoreType.DoubleNullable:
                    if (!asList)
                        return reader.ReadDoubleNullableArray(length);
                    else
                        return reader.ReadDoubleNullableList(length);
                case CoreType.DecimalNullable:
                    if (!asList)
                        return reader.ReadDecimalNullableArray(length);
                    else
                        return reader.ReadDecimalNullableList(length);
                case CoreType.CharNullable:
                    if (!asList)
                        return reader.ReadCharNullableArray(length);
                    else
                        return reader.ReadCharNullableList(length);
                case CoreType.DateTimeNullable:
                    if (!asList)
                        return reader.ReadDateTimeNullableArray(length);
                    else
                        return reader.ReadDateTimeNullableList(length);
                case CoreType.DateTimeOffsetNullable:
                    if (!asList)
                        return reader.ReadDateTimeOffsetNullableArray(length);
                    else
                        return reader.ReadDateTimeOffsetNullableList(length);
                case CoreType.TimeSpanNullable:
                    if (!asList)
                        return reader.ReadTimeSpanNullableArray(length);
                    else
                        return reader.ReadTimeSpanNullableList(length);
                case CoreType.GuidNullable:
                    if (!asList)
                        return reader.ReadGuidNullableArray(length);
                    else
                        return reader.ReadGuidNullableList(length);
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object FromBytesSpecialType(ref ByteReader reader, SerializerTypeDetails typeDetail, bool nullFlags)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var typeName = reader.ReadString(nullFlags);
                        if (typeName == null)
                            return null;
                        object value = Discovery.GetTypeFromName(typeName);
                        return value;
                    }
                case SpecialType.Dictionary:
                    {
                        if (nullFlags && reader.ReadIsNull())
                            return null;
                        var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false);
                        var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                        object value = Instantiator.CreateInstance(typeDetail.Type, new Type[] { innerItemEnumerable }, innerValue);
                        return value;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object FromBytesSpecialTypeEnumerable(ref ByteReader reader, int length, SerializerTypeDetails typeDetail, bool asList)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType.Value : typeDetail.TypeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        if (!asList)
                        {
                            var array = new Type[length];
                            for (int i = 0; i < length; i++)
                            {
                                var typeName = reader.ReadString(true);
                                if (typeName != null)
                                {
                                    var item = Discovery.GetTypeFromName(typeName);
                                    array[i] = item;
                                }
                            }
                            return array;
                        }
                        else
                        {
                            var list = new List<Type>(length);
                            for (int i = 0; i < length; i++)
                            {
                                var typeName = reader.ReadString(true);
                                if (typeName != null)
                                {
                                    var item = Discovery.GetTypeFromName(typeName);
                                    list.Add(item);
                                }
                                else
                                {
                                    list.Add(null);
                                }
                            }
                            return list;
                        }
                    }
                case SpecialType.Dictionary:
                    {
                        var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                        if (!asList)
                        {
                            var array = Array.CreateInstance(typeDetail.Type, length);
                            for (int i = 0; i < length; i++)
                            {
                                if (!reader.ReadIsNull())
                                {
                                    var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false);
                                    var item = Instantiator.CreateInstance(typeDetail.Type, new Type[] { innerItemEnumerable }, innerValue);
                                    array.SetValue(item, i);
                                }
                            }
                            return array;
                        }
                        else
                        {
                            var list = (IList)typeDetail.ListCreator(length);
                            for (int i = 0; i < length; i++)
                            {
                                if (!reader.ReadIsNull())
                                {
                                    var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false);
                                    var item = Instantiator.CreateInstance(typeDetail.Type, new Type[] { innerItemEnumerable }, innerValue);
                                    list.Add(item);
                                }
                                else
                                {
                                    list.Add(null);
                                }
                            }
                            return list;
                        }

                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}