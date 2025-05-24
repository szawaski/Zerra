// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

/// <summary>
/// An attribute for Enum values for a string represention.  Provides methods to convert Enums to strings and back.
/// Excluding the attribute means the string representation will be exact name of the value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class EnumName : Attribute
{
    /// <summary>
    /// The string representation of the Enum value.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Creats a new EnumName attribute for an Enum value with a string reprsentation.
    /// </summary>
    /// <param name="text">The string representation of the Enum value.</param>
    public EnumName(string text) => this.Text = text;

    private const char seperator = '|';

    private static readonly ConcurrentFactoryDictionary<Type, bool> hasFlags = new();
    private static bool HasFlagsAttribute(Type type)
    {
        return hasFlags.GetOrAdd(type, static (type) => type.GetTypeDetail().Attributes.Any(x => x is FlagsAttribute));
    }

    private static readonly ConcurrentFactoryDictionary<Type, CoreType> underlyingTypes = new();
    private static CoreType GetUnderlyingType(Type type)
    {
        return underlyingTypes.GetOrAdd(type, static (type) =>
        {
            if (!TypeLookup.CoreTypeLookup(Enum.GetUnderlyingType(type), out var underlyingType))
                throw new NotImplementedException("Should not happen");
            return underlyingType;
        });
    }

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<long, string>> nameCache = new();

    /// <summary>
    /// Get the string representation of an Enum using EnumName Attributes.
    /// </summary>
    /// <param name="type">The Enum type.</param>
    /// <param name="value">The Enum value.</param>
    /// <returns>The Enum as a string.</returns>
    /// <exception cref="ArgumentException">Throws if type is not an Enum.</exception>
    /// <exception cref="InvalidOperationException">Throws if the value is not a member of the Enum.</exception>
    public static string GetName(Type type, object value)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");

        var nameLookup = nameCache.GetOrAdd(type, static (type) =>
        {
            var items = new Dictionary<long, string>();
            var underlyingType = GetUnderlyingType(type);

            var typeDetail = type.GetTypeDetail();
            var values = Enum.GetValues(type);

            foreach (var enumValue in values)
            {
                var name = enumValue.ToString();
                if (name is null)
                    continue;

                var field = typeDetail.GetMember(name);
                foreach (var attribute in field.Attributes)
                {
                    if (attribute is EnumName enumNameAttribute && enumNameAttribute.Text is not null)
                        name = enumNameAttribute.Text;
                }

                long longValue;
                unchecked
                {
                    switch (underlyingType)
                    {
                        case CoreType.Byte:
                            longValue = (long)(byte)enumValue;
                            break;
                        case CoreType.SByte:
                            longValue = (long)(sbyte)enumValue;
                            break;
                        case CoreType.Int16:
                            longValue = (long)(short)enumValue;
                            break;
                        case CoreType.UInt16:
                            longValue = (long)(ushort)enumValue;
                            break;
                        case CoreType.Int32:
                            longValue = (long)(int)enumValue;
                            break;
                        case CoreType.UInt32:
                            longValue = (long)(uint)enumValue;
                            break;
                        case CoreType.Int64:
                            longValue = (long)enumValue;
                            break;
                        case CoreType.UInt64:
                            longValue = (long)(ulong)enumValue;
                            break;
                        default: throw new NotImplementedException();
                    }
                }

                items[longValue] = name; //Duplicate case take last ordered as C# does.
            }

            return items;
        });

        var underlyingType = GetUnderlyingType(type);

        long longValue;
        unchecked
        {
            switch (underlyingType)
            {
                case CoreType.Byte:
                    longValue = (long)(byte)value;
                    break;
                case CoreType.SByte:
                    longValue = (long)(sbyte)value;
                    break;
                case CoreType.Int16:
                    longValue = (long)(short)value;
                    break;
                case CoreType.UInt16:
                    longValue = (long)(ushort)value;
                    break;
                case CoreType.Int32:
                    longValue = (long)(int)value;
                    break;
                case CoreType.UInt32:
                    longValue = (long)(uint)value;
                    break;
                case CoreType.Int64:
                    longValue = (long)value;
                    break;
                case CoreType.UInt64:
                    longValue = (long)(ulong)value;
                    break;
                default: throw new NotImplementedException();
            }
        }

        if (nameLookup.TryGetValue(longValue, out var name))
            return name;

        if (HasFlagsAttribute(type))
        {
            lock (nameLookup)
            {
                if (nameLookup.TryGetValue(longValue, out name))
                    return name;

                var enumNames = Enum.GetNames(type);
                var sb = new StringBuilder();
                foreach (var enumName in enumNames)
                {
                    var enumValue = Enum.Parse(type, enumName);

                    long longEnumValue;
                    unchecked
                    {
                        switch (underlyingType)
                        {
                            case CoreType.Byte:
                                longEnumValue = (long)(byte)enumValue;
                                break;
                            case CoreType.SByte:
                                longEnumValue = (long)(sbyte)enumValue;
                                break;
                            case CoreType.Int16:
                                longEnumValue = (long)(short)enumValue;
                                break;
                            case CoreType.UInt16:
                                longEnumValue = (long)(ushort)enumValue;
                                break;
                            case CoreType.Int32:
                                longEnumValue = (long)(int)enumValue;
                                break;
                            case CoreType.UInt32:
                                longEnumValue = (long)(uint)enumValue;
                                break;
                            case CoreType.Int64:
                                longEnumValue = (long)enumValue;
                                break;
                            case CoreType.UInt64:
                                longEnumValue = (long)(ulong)enumValue;
                                break;
                            default: throw new NotImplementedException();
                        }
                    }

                    if (longEnumValue == 0)
                        continue;

                    var hasFlag = (longValue & longEnumValue) == longEnumValue;
                    if (!hasFlag)
                        continue;

                    var enumNameWithAttribute = enumName;
                    var typeDetail = type.GetTypeDetail();
                    var field = typeDetail.GetMember(enumName);
                    foreach (var attribute in field.Attributes)
                    {
                        if (attribute is EnumName enumNameAttribute && enumNameAttribute.Text is not null)
                            enumNameWithAttribute = enumNameAttribute.Text;
                    }

                    if (sb.Length > 0)
                        _ = sb.Append(seperator);
                    _ = sb.Append(enumNameWithAttribute);
                }

                name = sb.ToString();
                nameLookup.Add(longValue, name);
                return name;
            }
        }

        throw new InvalidOperationException($"Value {value.ToString()} is not found in enum {type.Name}");
    }
    /// <summary>
    /// Get the string representation of an Enum using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <param name="value">The Enum value.</param>
    /// <returns>The Enum as a string.</returns>
    /// <exception cref="InvalidOperationException">Throws if the value is not a member of the Enum.</exception>
    public static string GetName<T>(T value)
        where T : Enum
    {
        var type = typeof(T);
        if (type.Name == nameof(Enum))
            type = value.GetType();
        return GetName(type, value);
    }

    ///// <summary>
    ///// Gets all the string representations of an Enum using EnumName Attributes.
    ///// </summary>
    ///// <param name="type">The Enum type.</param>
    ///// <returns>All the Enum values as a string.</returns>
    ///// <exception cref="ArgumentException">Throws if type is not an Enum.</exception>
    //public static string[] GetNames(Type type)
    //{
    //    if (!type.IsEnum)
    //        throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");
    //    var namesLookup = GetNamesForType(type);
    //    return namesLookup.Values.ToArray();
    //}
    ///// <summary>
    ///// Gets all the string representations of an Enum using EnumName Attributes.
    ///// </summary>
    ///// <typeparam name="T">The Enum type.</typeparam>
    ///// <returns>All the Enum values as a string.</returns>
    ///// <exception cref="ArgumentException">Throws if type is not an Enum.</exception>
    //public static string[] GetNames<T>()
    //    where T : Enum
    //{
    //    return GetNames(typeof(T));
    //}

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<string, object>> valueLookups = new();
    private static Dictionary<string, object> GetValuesForType(Type type)
    {
        var valueLookup = valueLookups.GetOrAdd(type, static (type) =>
        {
            var items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var typeDetail = type.GetTypeDetail();
            foreach (var enumName in Enum.GetNames(type))
            {
                var enumValue = Enum.Parse(type, enumName);

                items[enumName] = enumValue;

                switch (typeDetail.EnumUnderlyingType!.Value)
                {
                    case CoreEnumType.Byte:
                    case CoreEnumType.ByteNullable:
                        items[((byte)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.SByte:
                    case CoreEnumType.SByteNullable:
                        items[((sbyte)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.Int16:
                    case CoreEnumType.Int16Nullable:
                        items[((short)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.UInt16:
                    case CoreEnumType.UInt16Nullable:
                        items[((ushort)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.Int32:
                    case CoreEnumType.Int32Nullable:
                        items[((int)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.UInt32:
                    case CoreEnumType.UInt32Nullable:
                        items[((uint)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.Int64:
                    case CoreEnumType.Int64Nullable:
                        items[((long)enumValue).ToString()] = enumValue;
                        break;
                    case CoreEnumType.UInt64:
                    case CoreEnumType.UInt64Nullable:
                        items[((ulong)enumValue).ToString()] = enumValue;
                        break;
                }

                var field = typeDetail.GetMember(enumName);

                string? enumNameWithAttribute = null;
                foreach (var attribute in field.Attributes)
                {
                    if (attribute is EnumName enumNameAttribute && enumNameAttribute.Text is not null)
                        enumNameWithAttribute = enumNameAttribute.Text;
                }
                if (enumNameWithAttribute is not null)
                {
#if NETSTANDARD2_0
                    if (!items.ContainsKey(enumNameWithAttribute))
                        items.Add(enumNameWithAttribute, enumValue);
#else
                    _ = items.TryAdd(enumNameWithAttribute, enumValue);
#endif
                }
            }
            return items;
        });
        return valueLookup;
    }

    /// <summary>
    /// Parses a string into an Enum while using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <param name="enumString">The string to parse to an Enum.</param>
    /// <returns>The resulting Enum value.</returns>
    /// <exception cref="InvalidOperationException">Throw if the Enum could not be parsed</exception>
    public static T Parse<T>(string? enumString)
    {
        return (T)Parse(enumString, typeof(T));
    }
    /// <summary>
    /// Parses a string into an Enum while using EnumName Attributes.
    /// </summary>
    /// <param name="enumString">The string to parse to an Enum.</param>
    /// <param name="type">The Enum type.</param>
    /// <returns>The resulting Enum value.</returns>
    /// <exception cref="InvalidOperationException">Throw if the Enum could not be parsed</exception>
    public static object Parse(string? enumString, Type type)
    {
        if (TryParse(enumString, type, out var value))
            return value;

        throw new InvalidOperationException($"Could not parse \"{enumString}\" into enum type {type.GetNiceName()}");
    }

    /// <summary>
    /// Attempts to parse a string into an Enum while using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <param name="enumString">The string to parse to an Enum.</param>
    /// <param name="value">The resulting Enum value if succesfully parsed.</param>
    /// <returns>True if the value was parsed; otherwise, False</returns>
    public static bool TryParse<T>(string? enumString,
#if !NETSTANDARD2_0
        [NotNullWhen(true)]
#endif
    out T? value)
        where T : Enum
    {
        if (TryParse(enumString, typeof(T), out var valueObject))
        {
            value = (T)valueObject;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
    /// <summary>
    /// Attempts to parse a string into an Enum while using EnumName Attributes.
    /// </summary>
    /// <param name="enumString">The string to parse to an Enum.</param>
    /// <param name="type">The Enum type.</param>
    /// <param name="value">The resulting Enum value if succesfully parsed.</param>
    /// <returns>True if the value was parsed; otherwise, False</returns>
    public static bool TryParse(string? enumString, Type type,
#if !NETSTANDARD2_0
        [NotNullWhen(true)]
#endif
    out object? value)
    {
        if (enumString is null)
        {
            value = null;
            return false;
        }
        var valueLookup = GetValuesForType(type);
        if (HasFlagsAttribute(type))
        {
            var found = false;
            var args = new object?[3];
            value = type.GetTypeDetail().CreatorBoxed();

            Span<char> chars = enumString.ToCharArray();
            var start = 0;
            var i = 0;
            for (; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c != seperator)
                    continue;

                var str = chars.Slice(start, i - start).ToString();
                start = i + 1;
                if (!valueLookup.TryGetValue(str, out var valueSplitItem))
                    continue;

                found = true;
                args[0] = type;
                args[1] = value;
                args[2] = valueSplitItem;
                var method = bitOrMethod.GetGenericMethodDetail(type);
                value = method.CallerBoxed(null, args);
            }
            if (i > start)
            {
                var str = chars.Slice(start, i - start).ToString();
                if (valueLookup.TryGetValue(str, out var valueSplitItem))
                {
                    found = true;
                    args[0] = type;
                    args[1] = value;
                    args[2] = valueSplitItem;
                    var method = bitOrMethod.GetGenericMethodDetail(type);
                    value = method.CallerBoxed(null, args);
                }
            }
            return found;
        }
        else
        {
            if (valueLookup.TryGetValue(enumString, out value))
                return true;

            value = default;
            return false;
        }
    }

    private static readonly MethodDetail bitOrMethod = typeof(EnumName).GetTypeDetail().GetMethodBoxed(nameof(BitOr));
    private static T BitOr<T>(Type type, object value1, object value2)
         where T : Enum
    {
        var underlyingType = GetUnderlyingType(type);

        unchecked
        {
            return underlyingType switch
            {
                CoreType.Byte => (T)(object)(byte)((byte)value1 | (byte)value2),
                CoreType.SByte => (T)(object)(sbyte)((sbyte)value1 | (sbyte)value2),
                CoreType.Int16 => (T)(object)(short)((short)value1 | (short)value2),
                CoreType.UInt16 => (T)(object)(ushort)((ushort)value1 | (ushort)value2),
                CoreType.Int32 => (T)(object)((int)value1 | (int)value2),
                CoreType.UInt32 => (T)(object)(uint)((uint)value1 | (uint)value2),
                CoreType.Int64 => (T)(object)((long)value1 | (long)value2),
                CoreType.UInt64 => (T)(object)((ulong)value1 | (ulong)value2),

                _ => throw new NotImplementedException(),
            };
        }
    }
}
