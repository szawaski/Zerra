// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializerOld
    {
        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (bytes == null || bytes.Length == 0)
                return default;

            return (T?)Deserialize(typeof(T), bytes, options);
        }
        public static object? Deserialize(Type type, ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            var typeDetail = GetTypeInformation(type, options.IndexSize, options.IgnoreIndexAttribute);

            var reader = new ByteReaderOld(bytes, options.Encoding);
            var obj = FromBytes(ref reader, typeDetail, true, false, ref optionsStruct);
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromBytes(ref ByteReaderOld reader, SerializerTypeDetail? typeDetail, bool nullFlags, bool drainBytes, ref OptionsStruct options)
        {
            if (options.IncludePropertyTypes)
            {
                var typeName = reader.ReadString(false);
                if (typeName == null)
                    throw new NotSupportedException("Cannot deserialize without type information");

                var typeFromBytes = Discovery.GetTypeFromName(typeName);

                if (typeFromBytes != null)
                {
                    var newTypeDetail = GetTypeInformation(typeFromBytes, options.IndexSize, options.IgnoreIndexAttribute);

                    //overrides potentially boxed type with actual type if exists in assembly
                    if (typeDetail != null)
                    {
                        var typeDetailCheck = typeDetail.TypeDetail;
                        if (typeDetailCheck.IsNullable)
                            typeDetailCheck = typeDetailCheck.InnerTypeDetail;
                        var newTypeDetailCheck = newTypeDetail.TypeDetail;

                        if (newTypeDetailCheck.Type != typeDetailCheck.Type && !newTypeDetailCheck.Interfaces.Contains(typeDetailCheck.Type) && !newTypeDetail.TypeDetail.BaseTypes.Contains(typeDetailCheck.Type))
                            throw new NotSupportedException($"{newTypeDetail.Type.GetNiceName()} does not convert to {typeDetail.TypeDetail.Type.GetNiceName()}");

                        typeDetail = newTypeDetail;
                    }
                }
            }
            else if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.TypeDetail.HasIEnumerableGeneric)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = GetTypeInformation(emptyImplementationType, options.IndexSize, options.IgnoreIndexAttribute);
            }

            if (typeDetail == null)
                throw new NotSupportedException("Cannot deserialize without type information");

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                return FromBytesCoreType(ref reader, typeDetail.TypeDetail.CoreType.Value, nullFlags);
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                var numValue = FromBytesCoreEnumType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, nullFlags);
                object? value;
                if (numValue != null)
                {
                    if (!typeDetail.TypeDetail.IsNullable)
                        value = Enum.ToObject(typeDetail.Type, numValue);
                    else
                        value = Enum.ToObject(typeDetail.TypeDetail.InnerType, numValue);
                }
                else
                {
                    value = null;
                }
                return value;
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                return FromBytesSpecialType(ref reader, typeDetail, nullFlags, ref options);
            }

            if (typeDetail.TypeDetail.HasIEnumerableGeneric)
            {
                var asList = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.HasIListGeneric;
                var asSet = !typeDetail.TypeDetail.Type.IsArray && typeDetail.TypeDetail.HasISetGeneric;
                var enumerable = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, asList, asSet, ref options);
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

            var obj = typeDetail.TypeDetail.HasCreatorBoxed ? typeDetail.TypeDetail.CreatorBoxed() : null;

            for (; ; )
            {
                SerializerMemberDetail? indexProperty = null;

                if (options.UsePropertyNames)
                {
                    var name = reader.ReadString(false);

                    if (name == String.Empty)
                        return obj;

                    _ = typeDetail.PropertiesByName?.TryGetValue(name!, out indexProperty);

                    if (indexProperty == null)
                    {
                        if (!options.UsePropertyNames && !options.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {name} undefined and no types.");

                        //consume bytes but object does not have property
                        var value = FromBytes(ref reader, null, false, true, ref options);
                    }
                    else
                    {
                        var value = FromBytes(ref reader, indexProperty.SerailzierTypeDetails, false, false, ref options);
                        if (obj != null)
                            indexProperty.Setter(obj, value);
                    }
                }
                else
                {
                    var propertyIndex = options.IndexSize switch
                    {
                        ByteSerializerIndexSize.Byte => (ushort)reader.ReadByte(),
                        ByteSerializerIndexSize.UInt16 => reader.ReadUInt16(),
                        _ => throw new NotImplementedException(),
                    };

                    if (propertyIndex == endObjectFlagUShort)
                        return obj;

                    if (typeDetail.PropertiesByIndex != null && typeDetail.PropertiesByIndex.Keys.Contains(propertyIndex))
                        indexProperty = typeDetail.PropertiesByIndex[propertyIndex];

                    if (indexProperty == null)
                    {
                        if (!options.UsePropertyNames && !options.IncludePropertyTypes)
                            throw new Exception($"Cannot deserialize with property {propertyIndex} undefined and no types.");

                        //consume bytes but object does not have property
                        var value = FromBytes(ref reader, null, false, true, ref options);
                    }
                    else
                    {
                        var value = FromBytes(ref reader, indexProperty.SerailzierTypeDetails, false, false, ref options);
                        if (obj != null)
                            indexProperty.Setter(obj, value);
                    }
                }
            }

            throw new Exception("Expected end of object marker");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromBytesEnumerable(ref ByteReaderOld reader, SerializerTypeDetail typeDetail, bool asList, bool asSet, ref OptionsStruct options)
        {
            var length = reader.ReadInt32();

            if (length == 0)
            {
                if (asList)
                    return typeDetail.ListCreator(length);
                else if (asSet)
                    return typeDetail.HashSetCreator(length);
                else
                    return Array.CreateInstance(typeDetail.Type, length);
            }

            if (typeDetail.TypeDetail.CoreType.HasValue)
            {
                return FromBytesCoreTypeEnumerable(ref reader, length, typeDetail.TypeDetail.CoreType.Value, asList, asSet);
            }

            if (typeDetail.TypeDetail.EnumUnderlyingType.HasValue)
            {
                if (asList)
                {
                    var list = (IList)typeDetail.ListCreator(length);
                    for (var i = 0; i < length; i++)
                    {
                        var numValue = FromBytesCoreEnumType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, true);
                        object? item;
                        if (numValue != null)
                        {
                            if (!typeDetail.TypeDetail.IsNullable)
                                item = Enum.ToObject(typeDetail.Type, numValue);
                            else
                                item = Enum.ToObject(typeDetail.TypeDetail.InnerType, numValue);
                        }
                        else
                        {
                            item = null;
                        }
                        _ = list.Add(item);
                    }
                    return list;
                }
                else if (asSet)
                {
                    var set = typeDetail.HashSetCreator(length);
                    var adder = typeDetail.HashSetAdder;
                    var adderArgs = new object?[1];
                    for (var i = 0; i < length; i++)
                    {
                        var numValue = FromBytesCoreEnumType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, true);
                        object? item;
                        if (numValue != null)
                        {
                            if (!typeDetail.TypeDetail.IsNullable)
                                item = Enum.ToObject(typeDetail.Type, numValue);
                            else
                                item = Enum.ToObject(typeDetail.TypeDetail.InnerType, numValue);
                        }
                        else
                        {
                            item = null;
                        }
                        adderArgs[0] = item;
                        adder.CallerBoxed(set, adderArgs);
                    }
                    return set;
                }
                else
                {
                    var array = Array.CreateInstance(typeDetail.Type, length);
                    for (var i = 0; i < length; i++)
                    {
                        var numValue = FromBytesCoreEnumType(ref reader, typeDetail.TypeDetail.EnumUnderlyingType.Value, true);
                        object? item;
                        if (numValue != null)
                        {
                            if (!typeDetail.TypeDetail.IsNullable)
                                item = Enum.ToObject(typeDetail.Type, numValue);
                            else
                                item = Enum.ToObject(typeDetail.TypeDetail.InnerType, numValue);
                        }
                        else
                        {
                            item = null;
                        }
                        array.SetValue(item, i);
                    }
                    return array;
                }
            }

            if (typeDetail.TypeDetail.SpecialType.HasValue || typeDetail.TypeDetail.IsNullable && typeDetail.InnerTypeDetail.TypeDetail.SpecialType.HasValue)
            {
                return FromBytesSpecialTypeEnumerable(ref reader, length, typeDetail, asList, ref options);
            }

            object? obj = null;

            if (asList)
            {
                var list = (IList)typeDetail.ListCreator(length);
                if (length == 0)
                    return list;

                var count = 0;
                for (; ; )
                {
                    var hasObject = reader.ReadBoolean();
                    if (!hasObject)
                    {
                        _ = list.Add(null);
                        count++;
                        if (count == length)
                            return list;
                        continue;
                    }

                    obj = FromBytes(ref reader, typeDetail, false, false, ref options);
                    _ = list.Add(obj);
                    count++;
                    if (count == length)
                        return list;
                }
            }
            else if (asSet)
            {
                var set = typeDetail.HashSetCreator(length);

                if (length == 0)
                    return set;

                var adder = typeDetail.HashSetAdder;
                var adderArgs = new object?[1];

                var count = 0;
                for (; ; )
                {
                    var hasObject = reader.ReadBoolean();
                    if (!hasObject)
                    {
                        adderArgs[0] = null;
                        adder.CallerBoxed(set, adderArgs);
                        count++;
                        if (count == length)
                            return set;
                        continue;
                    }

                    obj = FromBytes(ref reader, typeDetail, false, false, ref options);
                    adderArgs[0] = obj;
                    adder.CallerBoxed(set, adderArgs);
                    count++;
                    if (count == length)
                        return set;
                }
            }
            else
            {
                var array = Array.CreateInstance(typeDetail.Type, length);
                if (length == 0)
                    return array;

                var count = 0;
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

                    obj = FromBytes(ref reader, typeDetail, false, false, ref options);
                    array.SetValue(obj, count);
                    count++;
                    if (count == length)
                        return array;
                }
            }

            throw new Exception("Expected end of object marker");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromBytesCoreType(ref ByteReaderOld reader, CoreType coreType, bool nullFlags)
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
#if NET6_0_OR_GREATER
                CoreType.DateOnly => reader.ReadDateOnly(),
                CoreType.TimeOnly => reader.ReadTimeOnly(),
#endif
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
#if NET6_0_OR_GREATER
                CoreType.DateOnlyNullable => reader.ReadDateOnlyNullable(nullFlags),
                CoreType.TimeOnlyNullable => reader.ReadTimeOnlyNullable(nullFlags),
#endif
                CoreType.GuidNullable => reader.ReadGuidNullable(nullFlags),
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromBytesCoreEnumType(ref ByteReaderOld reader, CoreEnumType coreType, bool nullFlags)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless coreTypeCouldBeNull = true
            return coreType switch
            {
                CoreEnumType.Byte => reader.ReadByte(),
                CoreEnumType.SByte => reader.ReadSByte(),
                CoreEnumType.Int16 => reader.ReadInt16(),
                CoreEnumType.UInt16 => reader.ReadUInt16(),
                CoreEnumType.Int32 => reader.ReadInt32(),
                CoreEnumType.UInt32 => reader.ReadUInt32(),
                CoreEnumType.Int64 => reader.ReadInt64(),
                CoreEnumType.UInt64 => reader.ReadUInt64(),
                CoreEnumType.ByteNullable => reader.ReadByteNullable(nullFlags),
                CoreEnumType.SByteNullable => reader.ReadSByteNullable(nullFlags),
                CoreEnumType.Int16Nullable => reader.ReadInt16Nullable(nullFlags),
                CoreEnumType.UInt16Nullable => reader.ReadUInt16Nullable(nullFlags),
                CoreEnumType.Int32Nullable => reader.ReadInt32Nullable(nullFlags),
                CoreEnumType.UInt32Nullable => reader.ReadUInt32Nullable(nullFlags),
                CoreEnumType.Int64Nullable => reader.ReadInt64Nullable(nullFlags),
                CoreEnumType.UInt64Nullable => reader.ReadUInt64Nullable(nullFlags),
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromBytesCoreTypeEnumerable(ref ByteReaderOld reader, int length, CoreType coreType, bool asList, bool asSet)
        {
            //Core Types are skipped if null in an object property so null flags not necessary unless coreTypeCouldBeNull = true
            switch (coreType)
            {
                case CoreType.Boolean:
                    if (asList)
                        return reader.ReadBooleanList(length);
                    else if (asSet)
                        return reader.ReadBooleanHashSet(length);
                    else
                        return reader.ReadBooleanArray(length);
                case CoreType.Byte:
                    if (asList)
                        return reader.ReadByteList(length);
                    else if (asSet)
                        return reader.ReadByteHashSet(length);
                    else
                        return reader.ReadByteArray(length);
                case CoreType.SByte:
                    if (asList)
                        return reader.ReadSByteList(length);
                    else if (asSet)
                        return reader.ReadSByteHashSet(length);
                    else
                        return reader.ReadSByteArray(length);
                case CoreType.Int16:
                    if (asList)
                        return reader.ReadInt16List(length);
                    else if (asSet)
                        return reader.ReadInt16HashSet(length);
                    else
                        return reader.ReadInt16Array(length);
                case CoreType.UInt16:
                    if (asList)
                        return reader.ReadUInt16List(length);
                    else if (asSet)
                        return reader.ReadUInt16HashSet(length);
                    else
                        return reader.ReadUInt16Array(length);
                case CoreType.Int32:
                    if (asList)
                        return reader.ReadInt32List(length);
                    else if (asSet)
                        return reader.ReadInt32HashSet(length);
                    else
                        return reader.ReadInt32Array(length);
                case CoreType.UInt32:
                    if (asList)
                        return reader.ReadUInt32List(length);
                    else if (asSet)
                        return reader.ReadUInt32HashSet(length);
                    else
                        return reader.ReadUInt32Array(length);
                case CoreType.Int64:
                    if (asList)
                        return reader.ReadInt64List(length);
                    else if (asSet)
                        return reader.ReadInt64HashSet(length);
                    else
                        return reader.ReadInt64Array(length);
                case CoreType.UInt64:
                    if (asList)
                        return reader.ReadUInt64List(length);
                    else if (asSet)
                        return reader.ReadUInt64HashSet(length);
                    else
                        return reader.ReadUInt64Array(length);
                case CoreType.Single:
                    if (asList)
                        return reader.ReadSingleList(length);
                    else if (asSet)
                        return reader.ReadSingleHashSet(length);
                    else
                        return reader.ReadSingleArray(length);
                case CoreType.Double:
                    if (asList)
                        return reader.ReadDoubleList(length);
                    else if (asSet)
                        return reader.ReadDoubleHashSet(length);
                    else
                        return reader.ReadDoubleArray(length);
                case CoreType.Decimal:
                    if (asList)
                        return reader.ReadDecimalList(length);
                    else if (asSet)
                        return reader.ReadDecimalHashSet(length);
                    else
                        return reader.ReadDecimalArray(length);
                case CoreType.Char:
                    if (asList)
                        return reader.ReadCharList(length);
                    else if (asSet)
                        return reader.ReadCharHashSet(length);
                    else
                        return reader.ReadCharArray(length);
                case CoreType.DateTime:
                    if (asList)
                        return reader.ReadDateTimeList(length);
                    else if (asSet)
                        return reader.ReadDateTimeHashSet(length);
                    else
                        return reader.ReadDateTimeArray(length);
                case CoreType.DateTimeOffset:
                    if (asList)
                        return reader.ReadDateTimeOffsetList(length);
                    else if (asSet)
                        return reader.ReadDateTimeOffsetHashSet(length);
                    else
                        return reader.ReadDateTimeOffsetArray(length);
                case CoreType.TimeSpan:
                    if (asList)
                        return reader.ReadTimeSpanList(length);
                    else if (asSet)
                        return reader.ReadTimeSpanHashSet(length);
                    else
                        return reader.ReadTimeSpanArray(length);
#if NET6_0_OR_GREATER
                case CoreType.DateOnly:
                    if (asList)
                        return reader.ReadDateOnlyList(length);
                    else if (asSet)
                        return reader.ReadDateOnlyHashSet(length);
                    else
                        return reader.ReadDateOnlyArray(length);
                case CoreType.TimeOnly:
                    if (asList)
                        return reader.ReadTimeOnlyList(length);
                    else if (asSet)
                        return reader.ReadTimeOnlyHashSet(length);
                    else
                        return reader.ReadTimeOnlyArray(length);
#endif
                case CoreType.Guid:
                    if (asList)
                        return reader.ReadGuidList(length);
                    else if (asSet)
                        return reader.ReadGuidHashSet(length);
                    else
                        return reader.ReadGuidArray(length);

                case CoreType.String:
                    if (asList)
                        return reader.ReadStringList(length);
                    else if (asSet)
                        return reader.ReadStringHashSet(length);
                    else
                        return reader.ReadStringArray(length);

                case CoreType.BooleanNullable:
                    if (asList)
                        return reader.ReadBooleanNullableList(length);
                    else if (asSet)
                        return reader.ReadBooleanNullableHashSet(length);
                    else
                        return reader.ReadBooleanNullableArray(length);
                case CoreType.ByteNullable:
                    if (asList)
                        return reader.ReadByteNullableList(length);
                    else if (asSet)
                        return reader.ReadByteNullableHashSet(length);
                    else
                        return reader.ReadByteNullableArray(length);
                case CoreType.SByteNullable:
                    if (asList)
                        return reader.ReadSByteNullableList(length);
                    else if (asSet)
                        return reader.ReadSByteNullableHashSet(length);
                    else
                        return reader.ReadSByteNullableArray(length);
                case CoreType.Int16Nullable:
                    if (asList)
                        return reader.ReadInt16NullableList(length);
                    else if (asSet)
                        return reader.ReadInt16NullableHashSet(length);
                    else
                        return reader.ReadInt16NullableArray(length);
                case CoreType.UInt16Nullable:
                    if (asList)
                        return reader.ReadUInt16NullableList(length);
                    else if (asSet)
                        return reader.ReadUInt16NullableHashSet(length);
                    else
                        return reader.ReadUInt16NullableArray(length);
                case CoreType.Int32Nullable:
                    if (asList)
                        return reader.ReadInt32NullableList(length);
                    else if (asSet)
                        return reader.ReadInt32NullableHashSet(length);
                    else
                        return reader.ReadInt32NullableArray(length);
                case CoreType.UInt32Nullable:
                    if (asList)
                        return reader.ReadUInt32NullableList(length);
                    else if (asSet)
                        return reader.ReadUInt32NullableHashSet(length);
                    else
                        return reader.ReadUInt32NullableArray(length);
                case CoreType.Int64Nullable:
                    if (asList)
                        return reader.ReadInt64NullableList(length);
                    else if (asSet)
                        return reader.ReadInt64NullableHashSet(length);
                    else
                        return reader.ReadInt64NullableArray(length);
                case CoreType.UInt64Nullable:
                    if (asList)
                        return reader.ReadUInt64NullableList(length);
                    else if (asSet)
                        return reader.ReadUInt64NullableHashSet(length);
                    else
                        return reader.ReadUInt64NullableArray(length);
                case CoreType.SingleNullable:
                    if (asList)
                        return reader.ReadSingleNullableList(length);
                    else if (asSet)
                        return reader.ReadSingleNullableHashSet(length);
                    else
                        return reader.ReadSingleNullableArray(length);
                case CoreType.DoubleNullable:
                    if (asList)
                        return reader.ReadDoubleNullableList(length);
                    else if (asSet)
                        return reader.ReadDoubleNullableHashSet(length);
                    else
                        return reader.ReadDoubleNullableArray(length);
                case CoreType.DecimalNullable:
                    if (asList)
                        return reader.ReadDecimalNullableList(length);
                    else if (asSet)
                        return reader.ReadDecimalNullableHashSet(length);
                    else
                        return reader.ReadDecimalNullableArray(length);
                case CoreType.CharNullable:
                    if (asList)
                        return reader.ReadCharNullableList(length);
                    else if (asSet)
                        return reader.ReadCharNullableHashSet(length);
                    else
                        return reader.ReadCharNullableArray(length);
                case CoreType.DateTimeNullable:
                    if (asList)
                        return reader.ReadDateTimeNullableList(length);
                    else if (asSet)
                        return reader.ReadDateTimeNullableHashSet(length);
                    else
                        return reader.ReadDateTimeNullableArray(length);
                case CoreType.DateTimeOffsetNullable:
                    if (asList)
                        return reader.ReadDateTimeOffsetNullableList(length);
                    else if (asSet)
                        return reader.ReadDateTimeOffsetNullableHashSet(length);
                    else
                        return reader.ReadDateTimeOffsetNullableArray(length);
                case CoreType.TimeSpanNullable:
                    if (asList)
                        return reader.ReadTimeSpanNullableList(length);
                    else if (asSet)
                        return reader.ReadTimeSpanNullableHashSet(length);
                    else
                        return reader.ReadTimeSpanNullableArray(length);
#if NET6_0_OR_GREATER
                case CoreType.DateOnlyNullable:
                    if (asList)
                        return reader.ReadDateOnlyNullableList(length);
                    else if (asSet)
                        return reader.ReadDateOnlyNullableHashSet(length);
                    else
                        return reader.ReadDateOnlyNullableArray(length);
                case CoreType.TimeOnlyNullable:
                    if (asList)
                        return reader.ReadTimeOnlyNullableList(length);
                    else if (asSet)
                        return reader.ReadTimeOnlyNullableHashSet(length);
                    else
                        return reader.ReadTimeOnlyNullableArray(length);
#endif
                case CoreType.GuidNullable:
                    if (asList)
                        return reader.ReadGuidNullableList(length);
                    else if (asSet)
                        return reader.ReadGuidNullableHashSet(length);
                    else
                        return reader.ReadGuidNullableArray(length);
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromBytesSpecialType(ref ByteReaderOld reader, SerializerTypeDetail typeDetail, bool nullFlags, ref OptionsStruct options)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType!.Value : typeDetail.TypeDetail.SpecialType!.Value;

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
                        var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false, false, ref options);
                        var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, typeDetail.TypeDetail.IEnumerableGenericInnerType);
                        if (typeDetail.Type.IsInterface)
                        {
                            var dictionaryGenericType = TypeAnalyzer.GetGenericType(dictionaryType, (Type[])typeDetail.TypeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                            var value = Instantiator.Create(dictionaryGenericType, [innerItemEnumerable], innerValue);
                            return value;
                        }
                        else
                        {
                            var value = Instantiator.Create(typeDetail.Type, [innerItemEnumerable], innerValue);
                            return value;
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromBytesSpecialTypeEnumerable(ref ByteReaderOld reader, int length, SerializerTypeDetail typeDetail, bool asList, ref OptionsStruct options)
        {
            var specialType = typeDetail.TypeDetail.IsNullable ? typeDetail.InnerTypeDetail.TypeDetail.SpecialType!.Value : typeDetail.TypeDetail.SpecialType!.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        if (!asList)
                        {
                            var array = new Type?[length];
                            for (var i = 0; i < length; i++)
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
                            var list = new List<Type?>(length);
                            for (var i = 0; i < length; i++)
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
                            for (var i = 0; i < length; i++)
                            {
                                if (!reader.ReadIsNull())
                                {
                                    var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false, false, ref options);
                                    var item = Instantiator.Create(typeDetail.Type, [innerItemEnumerable], innerValue);
                                    array.SetValue(item, i);
                                }
                            }
                            return array;
                        }
                        else
                        {
                            if (typeDetail.TypeDetail.HasIListGeneric)
                            {
                                var list = (IList)typeDetail.ListCreator(length);
                                for (var i = 0; i < length; i++)
                                {
                                    if (!reader.ReadIsNull())
                                    {
                                        var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false, false, ref options);
                                        var item = Instantiator.Create(typeDetail.Type, [innerItemEnumerable], innerValue);
                                        _ = list.Add(item);
                                    }
                                    else
                                    {
                                        _ = list.Add(null);
                                    }
                                }
                                return list;
                            }
                            else
                            {
                                var set = typeDetail.HashSetCreator(length);
                                var adder = typeDetail.HashSetAdder;
                                var adderArgs = new object?[1];
                                for (var i = 0; i < length; i++)
                                {
                                    if (!reader.ReadIsNull())
                                    {
                                        var innerValue = FromBytesEnumerable(ref reader, typeDetail.InnerTypeDetail, false, false, ref options);
                                        var item = Instantiator.Create(typeDetail.Type, [innerItemEnumerable], innerValue);
                                        adderArgs[0] = item;
                                        adder.CallerBoxed(set, adderArgs);
                                    }
                                    else
                                    {
                                        adderArgs[0] = null;
                                        adder.CallerBoxed(set, adderArgs);
                                    }
                                }
                                return set;
                            }
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}