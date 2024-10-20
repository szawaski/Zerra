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
        return hasFlags.GetOrAdd(type, static (type) => type.GetCustomAttribute<FlagsAttribute>() is not null);
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
    private static Dictionary<long, string> GetNamesForType(Type type)
    {
        var nameLookup = nameCache.GetOrAdd(type, static (type) =>
        {
            var items = new Dictionary<long, string>();
            var fields = type.GetFields();
            var underlyingType = GetUnderlyingType(type);

            var values = Enum.GetValues(type);
            if (HasFlagsAttribute(type))
            {
                long maxValue = 0;
                foreach (var enumValue in values)
                {
                    unchecked
                    {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                        switch (underlyingType)
                        {
                            case CoreType.Byte: maxValue |= (byte)enumValue; break;
                            case CoreType.SByte: maxValue |= (sbyte)enumValue; break;
                            case CoreType.Int16: maxValue |= (short)enumValue; break;
                            case CoreType.UInt16: maxValue |= (ushort)enumValue; break;
                            case CoreType.Int32: maxValue |= (int)enumValue; break;
                            case CoreType.UInt32: maxValue |= (uint)enumValue; break;
                            case CoreType.Int64: maxValue |= (long)enumValue; break;
                            case CoreType.UInt64: maxValue |= (long)(ulong)enumValue; break;
                            default: throw new NotImplementedException();
                        }
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                    }
                }

                var sb = new StringBuilder();
                for (long value = 0; value < maxValue; value++)
                {
                    foreach (var enumValue in values)
                    {
                        unchecked
                        {
                            switch (underlyingType)
                            {
                                case CoreType.Byte:
                                    var byteValue = (byte)enumValue;
                                    if ((byteValue > 0 || value == 0) && (value & byteValue) == byteValue)
                                        break;
                                    continue;
                                case CoreType.SByte:
                                    var sbyteValue = (sbyte)enumValue;
                                    if ((sbyteValue > 0 || value == 0) && (value & sbyteValue) == sbyteValue)
                                        break;
                                    continue;
                                case CoreType.Int16:
                                    var shortValue = (short)enumValue;
                                    if ((shortValue > 0 || value == 0) && (value & shortValue) == shortValue)
                                        break;
                                    continue;
                                case CoreType.UInt16:
                                    var ushortValue = (ushort)enumValue;
                                    if ((ushortValue > 0 || value == 0) && (value & ushortValue) == ushortValue)
                                        break;
                                    continue;
                                case CoreType.Int32:
                                    var intValue = (int)enumValue;
                                    if ((intValue > 0 || value == 0) && (value & intValue) == intValue)
                                        break;
                                    continue;
                                case CoreType.UInt32:
                                    var uintValue = (uint)enumValue;
                                    if ((uintValue > 0 || value == 0) && (value & uintValue) == uintValue)
                                        break;
                                    continue;
                                case CoreType.Int64:
                                    var longValue = (long)enumValue;
                                    if ((longValue > 0 || value == 0) && (value & longValue) == longValue)
                                        break;
                                    continue;
                                case CoreType.UInt64:
                                    var ulongValue = (long)(ulong)enumValue;
                                    if ((ulongValue > 0 || value == 0) && (value & ulongValue) == ulongValue)
                                        break;
                                    continue;
                                default: throw new NotImplementedException();
                            }
                        }

                        var name = enumValue.ToString();
                        if (name is null)
                            continue;

                        var field = fields.First(x => x.Name == name);
                        var attribute = field.GetCustomAttribute<EnumName>(false);
                        if (attribute is not null && attribute.Text is not null)
                            name = attribute.Text;

                        if (sb.Length != 0)
                            _ = sb.Append(seperator);
                        _ = sb.Append(name);
                    }

                    var valueString = sb.ToString();
                    items.Add(value, valueString);
                    sb.Clear();
                }
            }
            else
            {
                foreach (var enumValue in values)
                {
                    var name = enumValue.ToString();
                    if (name is null)
                        continue;

                    var field = fields.First(x => x.Name == name);
                    var attribute = field.GetCustomAttribute<EnumName>(false);
                    if (attribute is not null && attribute.Text is not null)
                        name = attribute.Text;

                    unchecked
                    {
                        switch (underlyingType)
                        {
                            case CoreType.Byte:
                                items.Add((byte)enumValue, name);
                                break;
                            case CoreType.SByte:
                                items.Add((sbyte)enumValue, name);
                                break;
                            case CoreType.Int16:
                                items.Add((short)enumValue, name);
                                break;
                            case CoreType.UInt16:
                                items.Add((ushort)enumValue, name);
                                break;
                            case CoreType.Int32:
                                items.Add((int)enumValue, name);
                                break;
                            case CoreType.UInt32:
                                items.Add((uint)enumValue, name);
                                break;
                            case CoreType.Int64:
                                items.Add((long)enumValue, name);
                                break;
                            case CoreType.UInt64:
                                items.Add((long)(ulong)enumValue, name);
                                break;
                            default: throw new NotImplementedException();
                        }
                    }
                }
            }

            return items;
        });
        return nameLookup;
    }

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
        var namesLookup = GetNamesForType(type);
        var underlyingType = GetUnderlyingType(type);

        unchecked
        {
            string? name;
            switch (underlyingType)
            {
                case CoreType.Byte:
                    if (namesLookup.TryGetValue((byte)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(byte)value} is not found in enum {type.Name}");
                case CoreType.SByte:
                    if (namesLookup.TryGetValue((sbyte)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(sbyte)value} is not found in enum {type.Name}");
                case CoreType.Int16:
                    if (namesLookup.TryGetValue((short)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(short)value} is not found in enum {type.Name}");
                case CoreType.UInt16:
                    if (namesLookup.TryGetValue((ushort)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(ushort)value} is not found in enum {type.Name}");
                case CoreType.Int32:
                    if (namesLookup.TryGetValue((int)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(int)value} is not found in enum {type.Name}");
                case CoreType.UInt32:
                    if (namesLookup.TryGetValue((uint)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(uint)value} is not found in enum {type.Name}");
                case CoreType.Int64:
                    if (namesLookup.TryGetValue((long)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(long)value} is not found in enum {type.Name}");
                case CoreType.UInt64:
                    if (namesLookup.TryGetValue((long)(ulong)value, out name))
                        return name;
                    throw new InvalidOperationException($"Value {(long)(ulong)value} is not found in enum {type.Name}");
                default:
                    throw new NotImplementedException();
            };
        }
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

    /// <summary>
    /// Gets all the string representations of an Enum using EnumName Attributes.
    /// </summary>
    /// <param name="type">The Enum type.</param>
    /// <returns>All the Enum values as a string.</returns>
    /// <exception cref="ArgumentException">Throws if type is not an Enum.</exception>
    public static string[] GetNames(Type type)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");
        var namesLookup = GetNamesForType(type);
        return namesLookup.Values.ToArray();
    }
    /// <summary>
    /// Gets all the string representations of an Enum using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <returns>All the Enum values as a string.</returns>
    /// <exception cref="ArgumentException">Throws if type is not an Enum.</exception>
    public static string[] GetNames<T>()
        where T : Enum
    {
        return GetNames(typeof(T));
    }

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<string, object>> valueLookups = new();
    private static Dictionary<string, object> GetValuesForType(Type type)
    {
        var valueLookup = valueLookups.GetOrAdd(type, static (type) =>
        {
            var items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var fields = type.GetFields();
            var typeDetail = type.GetTypeDetail();
            foreach (var enumValue in Enum.GetValues(type))
            {
                var name = enumValue.ToString();
                if (name is null)
                    continue;

                var field = fields.First(x => x.Name == name);

                items.Add(name, enumValue);

                switch (typeDetail.EnumUnderlyingType!.Value)
                {
                    case CoreEnumType.Byte:
                    case CoreEnumType.ByteNullable:
                        items.Add(((byte)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.SByte:
                    case CoreEnumType.SByteNullable:
                        items.Add(((sbyte)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.Int16:
                    case CoreEnumType.Int16Nullable:
                        items.Add(((short)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.UInt16:
                    case CoreEnumType.UInt16Nullable:
                        items.Add(((ushort)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.Int32:
                    case CoreEnumType.Int32Nullable:
                        items.Add(((int)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.UInt32:
                    case CoreEnumType.UInt32Nullable:
                        items.Add(((uint)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.Int64:
                    case CoreEnumType.Int64Nullable:
                        items.Add(((long)enumValue).ToString(), enumValue);
                        break;
                    case CoreEnumType.UInt64:
                    case CoreEnumType.UInt64Nullable:
                        items.Add(((ulong)enumValue).ToString(), enumValue);
                        break;
                }

                var attribute = field.GetCustomAttribute<EnumName>(false);
                if (attribute is not null)
                {
#if NETSTANDARD2_0
                    if (!items.ContainsKey(attribute.Text))
                        items.Add(attribute.Text, enumValue);
#else
                    _ = items.TryAdd(attribute.Text, enumValue);
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
    public static T Parse<T>(string enumString)
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
    public static object Parse(string enumString, Type type)
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
    public static bool TryParse<T>(string enumString,
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
    public static bool TryParse(string enumString, Type type,
#if !NETSTANDARD2_0
        [NotNullWhen(true)]
#endif
    out object? value)
    {
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
