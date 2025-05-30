﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection
{
    public static class TypeAnalyzer
    {
        public static T? Convert<T>(object? obj) { return (T?)Convert(obj, typeof(T)); }
        public static object? Convert(object? obj, Type type)
        {
            if (!TypeLookup.CoreTypeLookup(type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {type.Name}");

            if (obj is null)
            {
                return coreType switch
                {
                    CoreType.Boolean => default(bool),
                    CoreType.Byte => default(byte),
                    CoreType.SByte => default(sbyte),
                    CoreType.UInt16 => default(ushort),
                    CoreType.Int16 => default(short),
                    CoreType.UInt32 => default(uint),
                    CoreType.Int32 => default(int),
                    CoreType.UInt64 => default(ulong),
                    CoreType.Int64 => default(long),
                    CoreType.Single => default(float),
                    CoreType.Double => default(double),
                    CoreType.Decimal => default(decimal),
                    CoreType.Char => default(char),
                    CoreType.DateTime => default(DateTime),
                    CoreType.DateTimeOffset => default(DateTimeOffset),
                    CoreType.TimeSpan => default(TimeSpan),
#if NET6_0_OR_GREATER
                    CoreType.DateOnly => default(DateOnly),
                    CoreType.TimeOnly => default(TimeOnly),
#endif
                    CoreType.Guid => default(Guid),
                    CoreType.String => default(string),
                    CoreType.BooleanNullable => null,
                    CoreType.ByteNullable => null,
                    CoreType.SByteNullable => null,
                    CoreType.UInt16Nullable => null,
                    CoreType.Int16Nullable => null,
                    CoreType.UInt32Nullable => null,
                    CoreType.Int32Nullable => null,
                    CoreType.UInt64Nullable => null,
                    CoreType.Int64Nullable => null,
                    CoreType.SingleNullable => null,
                    CoreType.DoubleNullable => null,
                    CoreType.DecimalNullable => null,
                    CoreType.CharNullable => null,
                    CoreType.DateTimeNullable => null,
                    CoreType.DateTimeOffsetNullable => null,
                    CoreType.TimeSpanNullable => null,
#if NET6_0_OR_GREATER
                    CoreType.DateOnlyNullable => null,
                    CoreType.TimeOnlyNullable => null,
#endif
                    CoreType.GuidNullable => null,
                    _ => throw new NotImplementedException($"Type conversion not available for {type.Name}"),
                };
            }
            else
            {
                return coreType switch
                {
                    CoreType.Boolean => System.Convert.ToBoolean(obj),
                    CoreType.Byte => System.Convert.ToByte(obj),
                    CoreType.SByte => System.Convert.ToSByte(obj),
                    CoreType.UInt16 => System.Convert.ToUInt16(obj),
                    CoreType.Int16 => System.Convert.ToInt16(obj),
                    CoreType.UInt32 => System.Convert.ToUInt32(obj),
                    CoreType.Int32 => System.Convert.ToInt32(obj),
                    CoreType.UInt64 => System.Convert.ToUInt64(obj),
                    CoreType.Int64 => System.Convert.ToInt64(obj),
                    CoreType.Single => System.Convert.ToSingle(obj),
                    CoreType.Double => System.Convert.ToDouble(obj),
                    CoreType.Decimal => System.Convert.ToDecimal(obj),
                    CoreType.Char => System.Convert.ToChar(obj),
                    CoreType.DateTime => System.Convert.ToDateTime(obj),
                    CoreType.DateTimeOffset => ConvertToDateTimeOffset(obj),
                    CoreType.TimeSpan => ConvertToTimeSpan(obj),
#if NET6_0_OR_GREATER
                    CoreType.DateOnly => ConvertToDateOnly(obj),
                    CoreType.TimeOnly => ConvertToTimeOnly(obj),
#endif
                    CoreType.Guid => ConvertToGuid(obj),
                    CoreType.String => System.Convert.ToString(obj),
                    CoreType.BooleanNullable => System.Convert.ToBoolean(obj),
                    CoreType.ByteNullable => System.Convert.ToByte(obj),
                    CoreType.SByteNullable => System.Convert.ToSByte(obj),
                    CoreType.UInt16Nullable => System.Convert.ToUInt16(obj),
                    CoreType.Int16Nullable => System.Convert.ToInt16(obj),
                    CoreType.UInt32Nullable => System.Convert.ToUInt32(obj),
                    CoreType.Int32Nullable => System.Convert.ToInt32(obj),
                    CoreType.UInt64Nullable => System.Convert.ToUInt64(obj),
                    CoreType.Int64Nullable => System.Convert.ToInt64(obj),
                    CoreType.SingleNullable => System.Convert.ToSingle(obj),
                    CoreType.DoubleNullable => System.Convert.ToDouble(obj),
                    CoreType.DecimalNullable => System.Convert.ToDecimal(obj),
                    CoreType.CharNullable => System.Convert.ToChar(obj),
                    CoreType.DateTimeNullable => System.Convert.ToDateTime(obj),
                    CoreType.DateTimeOffsetNullable => System.Convert.ToDateTime(obj),
                    CoreType.TimeSpanNullable => ConvertToTimeSpan(obj),
#if NET6_0_OR_GREATER
                    CoreType.DateOnlyNullable => ConvertToDateOnly(obj),
                    CoreType.TimeOnlyNullable => ConvertToTimeOnly(obj),
#endif
                    CoreType.GuidNullable => ConvertToGuid(obj),
                    _ => throw new NotImplementedException($"Type conversion not available for {type.Name}"),
                };
            }
        }

        private static Guid ConvertToGuid(object? obj)
        {
            if (obj is null)
                return Guid.Empty;
            return Guid.Parse(obj.ToString() ?? String.Empty);
        }


        private static TimeSpan ConvertToTimeSpan(object? obj)
        {
            if (obj is null)
                return TimeSpan.MinValue;
            return TimeSpan.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
#if NET6_0_OR_GREATER
        private static DateOnly ConvertToDateOnly(object? obj)
        {
            if (obj is null)
                return DateOnly.MinValue;
            return DateOnly.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
        private static TimeOnly ConvertToTimeOnly(object? obj)
        {
            if (obj is null)
                return TimeOnly.MinValue;
            return TimeOnly.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
#endif

        private static DateTimeOffset ConvertToDateTimeOffset(object? obj)
        {
            if (obj is null)
                return DateTimeOffset.MinValue;
            return DateTimeOffset.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.CurrentCulture);
        }

        private static readonly ConcurrentFactoryDictionary<Type, TypeDetail> typeDetailsByType = new();
        private static readonly Dictionary<Type, Func<TypeDetail>> typeDetailCreatorsByType = new();
        public static TypeDetail GetTypeDetail(Type type) => typeDetailsByType.GetOrAdd(type, static (type) => typeDetailCreatorsByType.TryGetValue(type, out var typeDetailCreator) ? typeDetailCreator() : TypeDetailRuntime<object>.New(type));
        public static void AddTypeDetailCreator(Type type, Func<TypeDetail> typeDetailCreator) => typeDetailCreatorsByType.Add(type, typeDetailCreator);
        internal static void ReplaceTypeDetail(TypeDetail typeDetail) => typeDetailsByType[typeDetail.Type] = typeDetail;

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail?> methodDetailsByType = new();
        public static MethodDetail GetMethodDetail(Type type, string name, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(name, type, parameterTypes);
            var method = methodDetailsByType.GetOrAdd(key, type, name, parameterTypes, static (type, name, parameterTypes) =>
            {
                var typeDetails = GetTypeDetail(type);
                foreach (var methodDetail in typeDetails.MethodDetailsBoxed.OrderBy(x => x.ParameterDetails.Count))
                {
                    if (methodDetail.Name == name && (parameterTypes is null || methodDetail.ParameterDetails.Count == parameterTypes.Length))
                    {
                        var match = true;
                        if (parameterTypes is not null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != methodDetail.ParameterDetails[i].Type.Name || parameterTypes[i].Namespace != methodDetail.ParameterDetails[i].Type.Namespace)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                            return methodDetail;
                    }
                }
                return null;
            });
            return method ?? throw new ArgumentException($"{type.GetNiceName()}.{name} method not found");
        }

        private static readonly ConcurrentFactoryDictionary<MethodInfo, MethodDetail?> methodDetailsByMethodInfo = new();
        public static MethodDetail GetMethodDetail(MethodInfo methodInfo)
        {
            if (methodInfo.ReflectedType is null)
                throw new ArgumentException("MethodInfo.ReflectedType cannot be null");

            var method = methodDetailsByMethodInfo.GetOrAdd(methodInfo, static (methodInfo) =>
                 MethodDetailRuntime<object>.New(methodInfo.ReflectedType!, methodInfo.Name, methodInfo, false, new()));
            return method ?? throw new ArgumentException($"{methodInfo.ReflectedType.GetNiceName()}.{methodInfo.Name} method not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?> constructorDetailsByType = new();
        public static ConstructorDetail GetConstructorDetail(Type type, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(type, parameterTypes);
            var constructor = constructorDetailsByType.GetOrAdd(key, type, parameterTypes, static (type, parameterTypes) =>
            {
                var typeDetails = GetTypeDetail(type);
                foreach (var constructorDetail in typeDetails.ConstructorDetailsBoxed)
                {
                    if (parameterTypes is null || constructorDetail.ParameterDetails.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes is not null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i] != constructorDetail.ParameterDetails[i].Type)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                            return constructorDetail;
                    }
                }
                return null;
            });
            return constructor ?? throw new ArgumentException($"{type.GetNiceName()} constructor not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetailsByMethod = new();
        public static MethodDetail GetGenericMethodDetail(MethodInfo method, params Type[] types)
        {
            if (method.ReflectedType is null)
                throw new ArgumentNullException("method.ReflectedType");
            var key = new TypeKey(method.ToString(), types);
            var genericMethod = genericMethodDetailsByMethod.GetOrAdd(key, method, types, static (method, types) =>
            {
                var generic = method.MakeGenericMethod(types);
                return MethodDetailRuntime<object>.New(method.ReflectedType!, method.Name, generic, false, new object());
            });
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetails = new();
        public static MethodDetail GetGenericMethodDetail(MethodDetail methodDetail, params Type[] types)
        {
            var key = new TypeKey(methodDetail.MethodInfo.ToString(), types);
            var genericMethod = genericMethodDetails.GetOrAdd(key, methodDetail, types, static (methodDetail, types) => GetGenericMethodDetail(methodDetail.MethodInfo, types));
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail> genericTypeDetails = new();
        public static TypeDetail GetGenericTypeDetail(TypeDetail typeDetail, params Type[] types)
        {
            var key = new TypeKey(typeDetail.Type, types);
            var genericType = genericTypeDetails.GetOrAdd(key, typeDetail, types, static (typeDetail, types) =>
            {
                var generic = typeDetail.Type.MakeGenericType(types);
                return GetTypeDetail(generic);
            });
            return genericType;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Type> genericTypesByType = new();
        public static Type GetGenericType(Type type, params Type[] types)
        {
            var key = new TypeKey(type, types);
            var genericType = genericTypesByType.GetOrAdd(key, type, types, static (type, types) => type.MakeGenericType(types));
            return genericType;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail> genericTypeDetailsByType = new();
        public static TypeDetail GetGenericTypeDetail(Type type, params Type[] types)
        {
            var key = new TypeKey(type, types);
            var genericTypeDetail = genericTypeDetailsByType.GetOrAdd(key, type, types, static (type, types) => GetTypeDetail(type.MakeGenericType(types)));
            return genericTypeDetail;
        }
    }
}