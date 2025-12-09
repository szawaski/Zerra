// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

/// <summary>
/// An attribute for Enum values for a string represention.  Provides methods to convert Enums to strings and back.
/// Excluding the attribute means the string representation will be exact name of the value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
#pragma warning disable CA1050 // Declare types in namespaces
public sealed class EnumName : Attribute
#pragma warning restore CA1050 // Declare types in namespaces
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
            throw new ArgumentException($"Type {type.Name} is not an Enum");

        var enumInfo = GetEnumInfo(type);

        long longValue;
        unchecked
        {
            longValue = enumInfo.UnderlyingType switch
            {
                CoreEnumType.Byte => (long)(byte)value,
                CoreEnumType.SByte => (long)(sbyte)value,
                CoreEnumType.Int16 => (long)(short)value,
                CoreEnumType.UInt16 => (long)(ushort)value,
                CoreEnumType.Int32 => (long)(int)value,
                CoreEnumType.UInt32 => (long)(uint)value,
                CoreEnumType.Int64 => (long)value,
                CoreEnumType.UInt64 => (long)(ulong)value,
                _ => throw new NotImplementedException(),
            };
        }

        if (enumInfo.NamesByValue.TryGetValue(longValue, out var name))
            return name;

        if (enumInfo.HasFlagsAttribute)
        {
            lock (enumInfo.NamesByValue)
            {
                if (enumInfo.NamesByValue.TryGetValue(longValue, out name))
                    return name;

                var sb = new StringBuilder();
                foreach (var enumField in enumInfo.Fields)
                {
                    long longEnumValue;
                    unchecked
                    {
                        longEnumValue = enumInfo.UnderlyingType switch
                        {
                            CoreEnumType.Byte => (long)(byte)enumField.Value,
                            CoreEnumType.SByte => (long)(sbyte)enumField.Value,
                            CoreEnumType.Int16 => (long)(short)enumField.Value,
                            CoreEnumType.UInt16 => (long)(ushort)enumField.Value,
                            CoreEnumType.Int32 => (long)(int)enumField.Value,
                            CoreEnumType.UInt32 => (long)(uint)enumField.Value,
                            CoreEnumType.Int64 => (long)enumField.Value,
                            CoreEnumType.UInt64 => (long)(ulong)enumField.Value,
                            _ => throw new NotImplementedException(),
                        };
                    }

                    if (longEnumValue == 0)
                        continue;

                    var hasFlag = (longValue & longEnumValue) == longEnumValue;
                    if (!hasFlag)
                        continue;

                    if (sb.Length > 0)
                        _ = sb.Append(seperator);
                    _ = sb.Append(enumField.Text ?? enumField.Name);
                }

                name = sb.ToString();
                enumInfo.NamesByValue.Add(longValue, name);
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
    //        throw new ArgumentException($"Type {type.Name} is not an Enum");
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

    //    private static readonly ConcurrentFactoryDictionary<Type, Dictionary<string, object>> valueLookups = new();
    //    private static Dictionary<string, object> GetValuesForType(Type type)
    //    {
    //        var valueLookup = valueLookups.GetOrAdd(type, static (type) =>
    //        {
    //            var items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    //            var underlyingType = EnumName.GetUnderlyingType(type);

    //            foreach (var enumValue in Enum.GetValues(type))
    //            {
    //                var enumName = enumValue.ToString()!;

    //                items[enumName] = enumValue;

    //                switch (underlyingType)
    //                {
    //                    case CoreEnumType.Byte:
    //                    case CoreEnumType.ByteNullable:
    //                        items[((byte)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.SByte:
    //                    case CoreEnumType.SByteNullable:
    //                        items[((sbyte)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.Int16:
    //                    case CoreEnumType.Int16Nullable:
    //                        items[((short)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.UInt16:
    //                    case CoreEnumType.UInt16Nullable:
    //                        items[((ushort)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.Int32:
    //                    case CoreEnumType.Int32Nullable:
    //                        items[((int)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.UInt32:
    //                    case CoreEnumType.UInt32Nullable:
    //                        items[((uint)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.Int64:
    //                    case CoreEnumType.Int64Nullable:
    //                        items[((long)enumValue).ToString()] = enumValue;
    //                        break;
    //                    case CoreEnumType.UInt64:
    //                    case CoreEnumType.UInt64Nullable:
    //                        items[((ulong)enumValue).ToString()] = enumValue;
    //                        break;
    //                }

    //                var attributes = enumValue.GetType().GetCustomAttributes(true);

    //                string? enumNameWithAttribute = null;
    //                foreach (var attribute in attributes)
    //                {
    //                    if (attribute is EnumName enumNameAttribute && enumNameAttribute.Text is not null)
    //                        enumNameWithAttribute = enumNameAttribute.Text;
    //                }
    //                if (enumNameWithAttribute is not null)
    //                {
    //#if NETSTANDARD2_0
    //                    if (!items.ContainsKey(enumNameWithAttribute))
    //                        items.Add(enumNameWithAttribute, enumValue);
    //#else
    //                    _ = items.TryAdd(enumNameWithAttribute, enumValue);
    //#endif
    //                }
    //            }
    //            return items;
    //        });
    //        return valueLookup;
    //    }

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

        throw new InvalidOperationException($"Could not parse \"{enumString}\" into enum type {type.Name}");
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
        [MaybeNullWhen(false)]
#endif
    out T value)
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
        [MaybeNullWhen(false)]
#endif
    out object value)
    {
        if (enumString is null)
        {
            value = null;
            return false;
        }

        var enumInfo = EnumName.GetEnumInfo(type);

        if (enumInfo.HasFlagsAttribute && enumString.Contains(seperator))
        {
            var found = false;

            value = enumInfo.Creator();

            Span<char> chars = enumString.ToCharArray();
            var start = 0;
            var i = 0;
            for (; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c != seperator)
                    continue;

                var str = chars[start..i].ToString();
                start = i + 1;
                if (!enumInfo.ValuesByName.TryGetValue(str, out var valueSplitItem))
                    continue;

                found = true;
                value = enumInfo.BitOr(value, valueSplitItem);
            }
            if (i > start)
            {
                var str = chars[start..i].ToString();
                if (enumInfo.ValuesByName.TryGetValue(str, out var valueSplitItem))
                {
                    found = true;
                    value = enumInfo.BitOr(value, valueSplitItem);
                }
            }
            return found;
        }
        else
        {
            if (enumInfo.ValuesByName.TryGetValue(enumString, out value))
                return true;

            value = default;
            return false;
        }
    }

    private static readonly MethodInfo bitOrMethod = typeof(EnumName).GetMethod(nameof(BitOrGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static object BitOr(Type type, CoreEnumType underlyingType, object value1, object value2)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException($"Cannot parse enums with flag attribute for {type.Name}.  Dynamic code generation is not supported in this build configuration.");
#pragma warning disable IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
        var genericMethod = bitOrMethod.MakeGenericMethod(type);
#pragma warning restore IL2060 // Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
        return genericMethod.Invoke(null, [underlyingType, value1, value2])!;
    }
    private static object CreateEnum(Type type)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException($"Cannot create enum for {type.Name}.  Dynamic code generation is not supported in this build configuration.");
#pragma warning disable IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        return Activator.CreateInstance(type)!;
#pragma warning restore IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
    }
    private static T BitOrGeneric<T>(CoreEnumType underlyingType, object value1, object value2)
         where T : Enum
    {
        unchecked
        {
            return underlyingType switch
            {
                CoreEnumType.Byte => (T)(object)(byte)((byte)value1 | (byte)value2),
                CoreEnumType.SByte => (T)(object)(sbyte)((sbyte)value1 | (sbyte)value2),
                CoreEnumType.Int16 => (T)(object)(short)((short)value1 | (short)value2),
                CoreEnumType.UInt16 => (T)(object)(ushort)((ushort)value1 | (ushort)value2),
                CoreEnumType.Int32 => (T)(object)((int)value1 | (int)value2),
                CoreEnumType.UInt32 => (T)(object)(uint)((uint)value1 | (uint)value2),
                CoreEnumType.Int64 => (T)(object)((long)value1 | (long)value2),
                CoreEnumType.UInt64 => (T)(object)((ulong)value1 | (ulong)value2),

                _ => throw new NotImplementedException(),
            };
        }
    }

    private static readonly ConcurrentFactoryDictionary<Type, EnumInfo> enumInfoCache = new();
    private static EnumInfo GetEnumInfo(Type type)
    {
        return enumInfoCache.GetOrAdd(type, (Func<Type, EnumInfo>)(static (type) =>
        {
            //Enum fields are never trimmed
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            var enumFieldInfos = new EnumFieldInfo[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var enumValue = field.GetValue(null)!;
                string? enumText = null;
                foreach (var attribute in field.GetCustomAttributes(true))
                {
                    if (attribute is EnumName enumNameAttribute && enumNameAttribute.Text is not null)
                        enumText = enumNameAttribute.Text;
                }
                enumFieldInfos[i] = new EnumFieldInfo(field.Name, enumText, enumValue);
            }
            if (!TypeLookup.GetCoreEnumType(Enum.GetUnderlyingType(type), out var underlyingType))
                throw new NotImplementedException("Should not happen");

            var hasFlagsAttribute = type.GetCustomAttributes(true).Any(x => x is FlagsAttribute);

            object creator() => EnumName.CreateEnum(type);
            object bitOr(object value1, object value2) => EnumName.BitOr(type, (CoreEnumType)underlyingType, value1, value2);
            var enumInfo = new EnumInfo((CoreEnumType)underlyingType, hasFlagsAttribute, enumFieldInfos, creator, bitOr);
            return enumInfo;
        }));
    }

    internal static void Register(Type type, CoreEnumType underlyingType, bool hasFlagsAttribute, EnumFieldInfo[] fields, Func<object> creator, Func<object, object, object> bitOr)
    {
        var enumInfo = new EnumInfo(underlyingType, hasFlagsAttribute, fields, creator, bitOr);
        if (!enumInfoCache.TryAdd(type, enumInfo))
            throw new InvalidOperationException($"Enum type {type.Name} is already registered");
    }

    private sealed class EnumInfo
    {
        public readonly CoreEnumType UnderlyingType;
        public readonly bool HasFlagsAttribute;
        public readonly EnumFieldInfo[] Fields;
        public readonly Func<object> Creator;
        public readonly Func<object, object, object> BitOr;

        public readonly Dictionary<string, object> ValuesByName;
        public readonly Dictionary<long, string> NamesByValue;

        public EnumInfo(CoreEnumType underlyingType, bool hasFlagsAttribute, EnumFieldInfo[] fields, Func<object> creator, Func<object, object, object> bitOr)
        {
            this.UnderlyingType = underlyingType;
            this.HasFlagsAttribute = hasFlagsAttribute;
            this.Fields = fields;
            this.Creator = creator;
            this.BitOr = bitOr;

            this.ValuesByName = new Dictionary<string, object>();
            this.NamesByValue = new Dictionary<long, string>();

            foreach (var field in fields)
            {
                this.ValuesByName[field.Name] = field.Value;
                if (field.Text != null)
                    this.ValuesByName[field.Text] = field.Value;

                long longValue;
                unchecked
                {
                    longValue = underlyingType switch
                    {
                        CoreEnumType.Byte => (long)(byte)field.Value,
                        CoreEnumType.SByte => (long)(sbyte)field.Value,
                        CoreEnumType.Int16 => (long)(short)field.Value,
                        CoreEnumType.UInt16 => (long)(ushort)field.Value,
                        CoreEnumType.Int32 => (long)(int)field.Value,
                        CoreEnumType.UInt32 => (long)(uint)field.Value,
                        CoreEnumType.Int64 => (long)field.Value,
                        CoreEnumType.UInt64 => (long)(ulong)field.Value,
                        _ => throw new NotImplementedException(),
                    };
                }
                this.NamesByValue[longValue] = field.Text ?? field.Name; //Duplicate case take last ordered as C# does.
            }
        }
    }

    public sealed class EnumFieldInfo
    {
        public readonly string Name;
        public readonly string? Text;
        public readonly object Value;
        public EnumFieldInfo(string name, string? text, object value)
        {
            this.Name = name;
            this.Text = text;
            this.Value = value;
        }
    }
}
