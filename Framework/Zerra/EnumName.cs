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
public class EnumName : Attribute
{
    public string Text { get; set; }
    public EnumName(string text) { this.Text = text; }

    private static readonly ConcurrentFactoryDictionary<Type, CoreType> underlyingTypes = new ConcurrentFactoryDictionary<Type, CoreType>();
    private static CoreType GetUnderlyingType(Type type)
    {
        return underlyingTypes.GetOrAdd(type, (t) =>
        {
            if (!TypeLookup.CoreTypeLookup(Enum.GetUnderlyingType(t), out CoreType underlyingType))
                throw new NotImplementedException("Should not happen");
            return underlyingType;
        });
    }
    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<long, string>> nameLookups = new ConcurrentFactoryDictionary<Type, Dictionary<long, string>>();
    private static Dictionary<long, string> GetNameLookup(Type type)
    {
        var nameLookup = nameLookups.GetOrAdd(type, (t) =>
        {
            var items = new Dictionary<long, string>();
            var fields = t.GetFields();
            var underlyingType = GetUnderlyingType(t);

            foreach (var enumValue in Enum.GetValues(t))
            {
                var name = enumValue.ToString();
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
    public static string LookupName(Type type, object value)
    {
        if (!type.IsEnum) throw new ArgumentException($"Type {type.GetNiceName()} is not an Enum");
        var nameLookup = GetNameLookup(type);
        var underlyingType = GetUnderlyingType(type);

        unchecked
        {
            return underlyingType switch
            {
                CoreType.Byte => nameLookup[(byte)value],
                CoreType.SByte => nameLookup[(sbyte)value],
                CoreType.Int16 => nameLookup[(int)value],
                CoreType.UInt16 => nameLookup[(uint)value],
                CoreType.Int32 => nameLookup[(int)value],
                CoreType.UInt32 => nameLookup[(uint)value],
                CoreType.Int64 => nameLookup[(long)value],
                CoreType.UInt64 => nameLookup[(long)(ulong)value],
                _ => throw new NotImplementedException(),
            };
        }
    }

    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<string, object>> valueLookups = new ConcurrentFactoryDictionary<Type, Dictionary<string, object>>();
    private static Dictionary<string, object> GetValueLookup(Type type)
    {
        var valueLookup = valueLookups.GetOrAdd(type, (t) =>
        {
            var items = new Dictionary<string, object>();
            var fields = t.GetFields();
            foreach (var enumValue in Enum.GetValues(t))
            {
                var name = enumValue.ToString();
                var field = fields.First(x => x.Name == name);
                items.Add(name.ToLower(), enumValue);
                var attribute = field.GetCustomAttribute<EnumName>(false);
                if (attribute != null && items.ContainsKey(attribute.Text))
                    items.Add(attribute.Text.ToLower(), enumValue);
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
        var valueLookup = GetValueLookup(type);
        if (valueLookup.TryGetValue(enumString.ToLower(), out object value))
            return value;

        throw new Exception($"Could not parse \"{enumString}\" into enum type {type.GetNiceName()}");
    }

    public static bool TryParse<T>(string enumString, out T value)
        where T : Enum
    {
        if (TryParse(enumString, typeof(T), out object valueObject))
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
    public static bool TryParse(string enumString, Type type, out object value)
    {
        var valueLookup = GetValueLookup(type);
        if (valueLookup.TryGetValue(enumString.ToLower(), out value))
            return true;

        value = default;
        return false;
    }

    private static readonly ConcurrentFactoryDictionary<object, Type> typeLookup = new ConcurrentFactoryDictionary<object, Type>();
    public static string GetEnumName<T>(T value)
        where T : Enum
    {
        var type = typeLookup.GetOrAdd(value, (o) => { return o.GetType(); });
        return LookupName(type, value);
    }
}

public static class EnumNameExtensions
{
    public static string EnumName<T>(this T value)
        where T : Enum
    {
        return global::EnumName.GetEnumName(value);
    }
}