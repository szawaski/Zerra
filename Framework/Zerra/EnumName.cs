// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection;

[AttributeUsage(AttributeTargets.Field)]
public sealed class EnumName : Attribute
{
    public string Text { get; set; }
    public EnumName(string text) { this.Text = text; }

    private static readonly ConcurrentFactoryDictionary<Type, CoreType> underlyingTypes = new();
    private static CoreType GetUnderlyingType(Type type)
    {
        return underlyingTypes.GetOrAdd(type, (type) =>
        {
            if (!TypeLookup.CoreTypeLookup(Enum.GetUnderlyingType(type), out var underlyingType))
                throw new NotImplementedException("Should not happen");
            return underlyingType;
        });
    }

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<long, string>> nameCache = new();
    private static Dictionary<long, string> GetNamesForType(Type type)
    {
        var nameLookup = nameCache.GetOrAdd(type, (type) =>
        {
            var items = new Dictionary<long, string>();
            var fields = type.GetFields();
            var underlyingType = GetUnderlyingType(type);

            foreach (var enumValue in Enum.GetValues(type))
            {
                var name = enumValue.ToString();
                if (name == null)
                    continue;

                var field = fields.First(x => x.Name == name);
                var attribute = field.GetCustomAttribute<EnumName>(false);

                unchecked
                {
                    switch (underlyingType)
                    {
                        case CoreType.Byte:
                            if (attribute != null)
                                items.Add((byte)enumValue, (string)attribute.Text);
                            else
                                items.Add((byte)enumValue, name);
                            break;
                        case CoreType.SByte:
                            if (attribute != null)
                                items.Add((sbyte)enumValue, (string)attribute.Text);
                            else
                                items.Add((sbyte)enumValue, name);
                            break;
                        case CoreType.Int16:
                            if (attribute != null)
                                items.Add((int)enumValue, (string)attribute.Text);
                            else
                                items.Add((int)enumValue, name);
                            break;
                        case CoreType.UInt16:
                            if (attribute != null)
                                items.Add((short)enumValue, (string)attribute.Text);
                            else
                                items.Add((ushort)enumValue, name);
                            break;
                        case CoreType.Int32:
                            if (attribute != null)
                                items.Add((int)enumValue, (string)attribute.Text);
                            else
                                items.Add((int)enumValue, name);
                            break;
                        case CoreType.UInt32:
                            if (attribute != null)
                                items.Add((uint)enumValue, (string)attribute.Text);
                            else
                                items.Add((uint)enumValue, name);
                            break;
                        case CoreType.Int64:
                            if (attribute != null)
                                items.Add((long)enumValue, (string)attribute.Text);
                            else
                                items.Add((long)enumValue, name);
                            break;
                        case CoreType.UInt64:
                            if (attribute != null)
                                items.Add((long)(ulong)enumValue, (string)attribute.Text);
                            else
                                items.Add((long)(ulong)enumValue, name);
                            break;
                        default: throw new NotImplementedException();
                    }
                }
            }

            return items;
        });
        return nameLookup;
    }

    public static string GetName(Type type, object value)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");
        var namesLookup = GetNamesForType(type);
        var underlyingType = GetUnderlyingType(type);

        unchecked
        {
            return underlyingType switch
            {
                CoreType.Byte => namesLookup[(byte)value],
                CoreType.SByte => namesLookup[(sbyte)value],
                CoreType.Int16 => namesLookup[(int)value],
                CoreType.UInt16 => namesLookup[(uint)value],
                CoreType.Int32 => namesLookup[(int)value],
                CoreType.UInt32 => namesLookup[(uint)value],
                CoreType.Int64 => namesLookup[(long)value],
                CoreType.UInt64 => namesLookup[(long)(ulong)value],
                _ => throw new NotImplementedException(),
            };
        }
    }
    public static string GetName<T>(T value)
        where T : Enum
    {
        var type = typeof(T);
        if (type.Name == nameof(Enum))
            type = value.GetType();
        return GetName(type, value);
    }
    public static string[] GetNames(Type type)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");
        var namesLookup = GetNamesForType(type);
        return namesLookup.Values.ToArray();
    }
    public static string[] GetNames<T>()
        where T : Enum
    {
        return GetNames(typeof(T));
    }

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<string, object>> valueLookups = new();
    private static Dictionary<string, object> GetValuesForType(Type type)
    {
        var valueLookup = valueLookups.GetOrAdd(type, (type) =>
        {
            var items = new Dictionary<string, object>();
            var fields = type.GetFields();
            foreach (var enumValue in Enum.GetValues(type))
            {
                var name = enumValue.ToString();
                if (name == null)
                    continue;

                var field = fields.First(x => x.Name == name);
                items.Add(name.ToLower(), enumValue);
                var attribute = field.GetCustomAttribute<EnumName>(false);
                if (attribute != null)
                {
                    var attributeName = attribute.Text.ToLower();
                    if (!items.ContainsKey(attributeName))
                        items.Add(attributeName, enumValue);
                }
            }
            return items;
        });
        return valueLookup;
    }

    public static T Parse<T>(string enumString)
    {
        return (T)Parse(enumString, typeof(T));
    }
    public static object Parse(string enumString, Type type)
    {
        var valueLookup = GetValuesForType(type);
        if (valueLookup.TryGetValue(enumString.ToLower(), out var value))
            return value;

        throw new Exception($"Could not parse \"{enumString}\" into enum type {type.GetNiceName()}");
    }

    public static bool TryParse<T>(string enumString, out T? value)
        where T : Enum
    {
        if (TryParse(enumString, typeof(T), out var valueObject))
        {
            value = (T?)valueObject;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
    public static bool TryParse(string enumString, Type type, out object? value)
    {
        var valueLookup = GetValuesForType(type);
        if (valueLookup.TryGetValue(enumString.ToLower(), out value))
            return true;

        value = default;
        return false;
    }
}
