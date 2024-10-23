// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public class JsonObject : IEnumerable
    {
        private readonly JsonObjectType jsonType;
        private readonly bool valueBoolean;
        private readonly decimal valueNumber;
        private readonly string? valueString;
        private readonly Dictionary<string, JsonObject>? valueProperties;
        private readonly List<JsonObject>? valueArray;

        public JsonObject()
        {
            jsonType = JsonObjectType.Null;
        }

        public JsonObject(bool value)
        {
            jsonType = JsonObjectType.Boolean;
            valueBoolean = value;
        }

        public JsonObject(decimal value)
        {
            jsonType = JsonObjectType.Number;
            valueNumber = value;
        }

        public JsonObject(string value)
        {
            jsonType = JsonObjectType.String;
            valueString = value;
        }

        public JsonObject(Dictionary<string, JsonObject> value)
        {
            jsonType = JsonObjectType.Object;
            valueProperties = value;
        }

        public JsonObject(List<JsonObject> value)
        {
            jsonType = JsonObjectType.Array;
            valueArray = value;
        }

        public enum JsonObjectType
        {
            Null,
            Boolean,
            Number,
            String,
            Object,
            Array
        }

        public JsonObjectType JsonType => jsonType;
        public bool IsNull => jsonType == JsonObjectType.Null;

        public JsonObject this[string property]
        {
            get
            {
                if (jsonType != JsonObjectType.Object)
                    throw new InvalidCastException();
                if (!valueProperties!.TryGetValue(property, out var obj))
                    throw new ArgumentException();
                return obj;
            }
            set
            {
                if (jsonType != JsonObjectType.Object)
                    throw new InvalidCastException();
                valueProperties![property] = value;
            }
        }
        public JsonObject this[int index]
        {
            get
            {
                if (jsonType != JsonObjectType.Array)
                    throw new InvalidCastException();
                return valueArray![index];
            }
            set
            {
                if (jsonType != JsonObjectType.Array)
                    throw new InvalidCastException();
                valueArray![index] = value;
            }
        }
        public void Add(JsonObject jsonObject)
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            valueArray!.Add(jsonObject);
        }
        public bool Remove(JsonObject jsonObject)
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.Remove(jsonObject);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }
        private void ToString(StringBuilder sb)
        {
            switch (jsonType)
            {
                case JsonObjectType.Null:
                    {
                        _ = sb.Append("null");
                    }
                    break;
                case JsonObjectType.Boolean:
                    {
                        _ = sb.Append(valueBoolean ? "true" : "false");
                    }
                    break;
                case JsonObjectType.Number:
                    {
                        _ = sb.Append(valueNumber);
                    }
                    break;
                case JsonObjectType.String:
                    {
                        ToStringEncoded(valueString, sb);
                    }
                    break;
                case JsonObjectType.Object:
                    {
                        _ = sb.Append('{');
                        var first = true;
                        foreach (var item in valueProperties!)
                        {
                            if (first)
                                first = false;
                            else
                                _ = sb.Append(',');
                            ToStringEncoded(item.Key, sb);
                            sb.Append(':');
                            item.Value.ToString(sb);
                        }
                        _ = sb.Append('}');
                    }
                    break;
                case JsonObjectType.Array:
                    {
                        _ = sb.Append('[');
                        var first = true;
                        foreach (var item in valueArray!)
                        {
                            if (first)
                                first = false;
                            else
                                _ = sb.Append(',');
                            item.ToString(sb);
                        }
                        _ = sb.Append(']');
                    }
                    break;
            }
        }
        private static void ToStringEncoded(string? value, StringBuilder sb)
        {
            if (value is null)
            {
                _ = sb.Append("null");
                return;
            }
            _ = sb.Append('\"');
            if (value.Length == 0)
            {
                _ = sb.Append('\"');
                return;
            }

            var chars = value.AsSpan();

            var start = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                char escapedChar;
                switch (c)
                {
                    case '"':
                        escapedChar = '"';
                        break;
                    case '\\':
                        escapedChar = '\\';
                        break;
                    case '\b':
                        escapedChar = 'b';
                        break;
                    case '\f':
                        escapedChar = 'f';
                        break;
                    case '\n':
                        escapedChar = 'n';
                        break;
                    case '\r':
                        escapedChar = 'r';
                        break;
                    case '\t':
                        escapedChar = 't';
                        break;
                    default:
                        if (c >= ' ')
                            continue;

#if NETSTANDARD2_0
                        _ = sb.Append(chars.Slice(start, i - start).ToArray());
#else
                        _ = sb.Append(chars.Slice(start, i - start));
#endif
                        start = i + 1;
                        var code = StringHelper.LowUnicodeIntToEncodedHex[c];
                        _ = sb.Append(code);
                        continue;
                }
#if NETSTANDARD2_0
                _ = sb.Append(chars.Slice(start, i - start).ToArray());
#else
                _ = sb.Append(chars.Slice(start, i - start));
#endif

                start = i + 1;
                _ = sb.Append('\\');
                _ = sb.Append(escapedChar);
            }

            if (start != chars.Length)
#if NETSTANDARD2_0
                _ = sb.Append(chars.Slice(start, chars.Length - start).ToArray());
#else
                _ = sb.Append(chars.Slice(start, chars.Length - start));
#endif
            sb.Append('\"');
        }

        public static explicit operator bool(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Boolean)
                throw new InvalidCastException();
            return obj.valueBoolean;
        }
        public static explicit operator byte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (byte)obj.valueNumber;
        }
        public static explicit operator sbyte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (sbyte)obj.valueNumber;
        }
        public static explicit operator short(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (short)obj.valueNumber;
        }
        public static explicit operator ushort(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ushort)obj.valueNumber;
        }
        public static explicit operator int(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (int)obj.valueNumber;
        }
        public static explicit operator uint(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (uint)obj.valueNumber;
        }
        public static explicit operator long(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (long)obj.valueNumber;
        }
        public static explicit operator ulong(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ulong)obj.valueNumber;
        }
        public static explicit operator float(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (float)obj.valueNumber;
        }
        public static explicit operator double(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (double)obj.valueNumber;
        }
        public static explicit operator decimal(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return obj.valueNumber;
        }
        public static explicit operator char(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            if (obj.valueString!.Length == 0)
                throw new InvalidCastException();
            return obj.valueString[0];
        }
        public static explicit operator DateTime(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTime.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator DateTimeOffset(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTimeOffset.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator TimeSpan(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeSpan.Parse(obj.valueString!);
        }
#if NET6_0_OR_GREATER
        public static explicit operator DateOnly(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateOnly.Parse(obj.valueString!);
        }
        public static explicit operator TimeOnly(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeOnly.Parse(obj.valueString!);
        }
#endif
        public static explicit operator Guid(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return Guid.Parse(obj.valueString!);
        }

        public static explicit operator string?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return obj.valueString;
        }

        public static explicit operator bool?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Boolean)
                throw new InvalidCastException();
            return obj.valueBoolean;
        }
        public static explicit operator byte?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (byte)obj.valueNumber;
        }
        public static explicit operator sbyte?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (sbyte)obj.valueNumber;
        }
        public static explicit operator short?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (short)obj.valueNumber;
        }
        public static explicit operator ushort?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ushort)obj.valueNumber;
        }
        public static explicit operator int?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (int)obj.valueNumber;
        }
        public static explicit operator uint?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (uint)obj.valueNumber;
        }
        public static explicit operator long?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (long)obj.valueNumber;
        }
        public static explicit operator ulong?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ulong)obj.valueNumber;
        }
        public static explicit operator float?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (float)obj.valueNumber;
        }
        public static explicit operator double?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (double)obj.valueNumber;
        }
        public static explicit operator decimal?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (decimal)obj.valueNumber;
        }
        public static explicit operator char?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            if (obj.valueString!.Length == 0)
                throw new InvalidCastException();
            return obj.valueString[0];
        }
        public static explicit operator DateTime?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTime.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator DateTimeOffset?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTimeOffset.Parse(obj.valueString!);
        }
        public static explicit operator TimeSpan?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeSpan.Parse(obj.valueString!);
        }
#if NET6_0_OR_GREATER
        public static explicit operator DateOnly?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateOnly.Parse(obj.valueString!);
        }
        public static explicit operator TimeOnly?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeOnly.Parse(obj.valueString!);
        }
#endif

        public static explicit operator JsonObject[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return obj.valueArray!.ToArray();
        }
        public static explicit operator List<JsonObject>?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return obj.valueArray!;
        }

        public IEnumerator<JsonObject> GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return ((IEnumerable<JsonObject>)valueArray!).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.GetEnumerator();
        }

        private static readonly Type plainListType = typeof(List<>);
        private static readonly Type plainSetType = typeof(HashSet<>);
        public T? Bind<T>() { return (T?)Bind(typeof(T)); }
        public object? Bind(Type type)
        {
            if (IsNull)
                return null;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            if (typeDetail.CoreType.HasValue)
            {
                return typeDetail.CoreType.Value switch
                {
                    CoreType.Boolean => (bool)this,
                    CoreType.Byte => (byte)this,
                    CoreType.SByte => (sbyte)this,
                    CoreType.Int16 => (short)this,
                    CoreType.UInt16 => (ushort)this,
                    CoreType.Int32 => (int)this,
                    CoreType.UInt32 => (uint)this,
                    CoreType.Int64 => (long)this,
                    CoreType.UInt64 => (ulong)this,
                    CoreType.Single => (float)this,
                    CoreType.Double => (double)this,
                    CoreType.Decimal => (decimal)this,
                    CoreType.Char => (char)this,
                    CoreType.DateTime => (DateTime)this,
                    CoreType.DateTimeOffset => (DateTimeOffset)this,
                    CoreType.TimeSpan => (TimeSpan)this,
#if NET6_0_OR_GREATER
                    CoreType.DateOnly => (DateOnly)this,
                    CoreType.TimeOnly => (TimeOnly)this,
#endif
                    CoreType.Guid => (Guid)this,
                    CoreType.String => (string?)this,
                    CoreType.BooleanNullable => (bool?)this,
                    CoreType.ByteNullable => (byte?)this,
                    CoreType.SByteNullable => (sbyte?)this,
                    CoreType.Int16Nullable => (short?)this,
                    CoreType.UInt16Nullable => (ushort?)this,
                    CoreType.Int32Nullable => (int?)this,
                    CoreType.UInt32Nullable => (uint?)this,
                    CoreType.Int64Nullable => (long?)this,
                    CoreType.UInt64Nullable => (ulong?)this,
                    CoreType.SingleNullable => (float?)this,
                    CoreType.DoubleNullable => (double?)this,
                    CoreType.DecimalNullable => (decimal?)this,
                    CoreType.CharNullable => (char?)this,
                    CoreType.DateTimeNullable => (DateTime?)this,
                    CoreType.DateTimeOffsetNullable => (DateTimeOffset?)this,
                    CoreType.TimeSpanNullable => (TimeSpan?)this,
#if NET6_0_OR_GREATER
                    CoreType.DateOnlyNullable => (DateOnly?)this,
                    CoreType.TimeOnlyNullable => (TimeOnly?)this,
#endif
                    CoreType.GuidNullable => (Guid?)this,
                    _ => throw new NotImplementedException(),
                };
            }

            if (typeDetail.Type.IsEnum)
            {
                if (jsonType != JsonObjectType.String)
                    throw new InvalidOperationException();
                return Enum.Parse(type, valueString!);
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypeDetail.Type.IsEnum)
            {
                if (jsonType != JsonObjectType.String)
                    throw new InvalidOperationException();
                if (valueString == String.Empty)
                    return null;
                return Enum.Parse(typeDetail.InnerTypeDetail.Type, valueString!);
            }

            if (typeDetail.HasIDictionaryGeneric || typeDetail.HasIReadOnlyDictionaryGeneric)
            {
                if (jsonType != JsonObjectType.Object)
                    throw new InvalidCastException();
                IDictionary dictionary;
                if (typeDetail.Type.IsInterface)
                {
                    var dictionaryType = TypeAnalyzer.GetGenericTypeDetail(typeof(Dictionary<,>), (Type[])typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                    dictionary = (IDictionary)dictionaryType.CreatorBoxed();
                }
                else
                {
                    dictionary = (IDictionary)typeDetail.CreatorBoxed();
                }
                foreach (var item in valueProperties!)
                {
                    var key = TypeAnalyzer.Convert(item.Key, typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes[0]);
                    var value = item.Value.Bind(typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes[1]);
                    dictionary.Add(key!, value);
                }
                return dictionary;
            }

            if (typeDetail.HasIEnumerableGeneric)
            {
                if (typeDetail.Type == typeof(byte[]))
                {
                    if (jsonType != JsonObjectType.String)
                        throw new InvalidCastException();
                    if (valueString == String.Empty)
                        return Array.Empty<byte>();
                    return Convert.FromBase64String(valueString!);
                }
                else
                {
                    if (jsonType != JsonObjectType.Array)
                        throw new InvalidCastException();
                    var innerType = typeDetail.InnerTypeDetail.Type;
                    if (!typeDetail.Type.IsArray && typeDetail.HasIListGeneric)
                    {
                        IList list;
                        if (typeDetail.Type.IsInterface)
                        {
                            var listType = TypeAnalyzer.GetGenericTypeDetail(plainListType, innerType);
                            list = (IList)listType.CreatorBoxed();
                        }
                        else
                        {
                            list = (IList)typeDetail.CreatorBoxed();
                        }
                        foreach (var item in valueArray!)
                        {
                            var value = item.Bind(innerType);
                            _ = list.Add(value);
                        }
                        return list;
                    }
                    if (!typeDetail.Type.IsArray && typeDetail.HasISetGeneric)
                    {
                        object set;
                        if (typeDetail.Type.IsInterface)
                        {
                            var setType = TypeAnalyzer.GetGenericTypeDetail(plainSetType, innerType);
                            set = setType.CreatorBoxed();
                        }
                        else
                        {
                            set = typeDetail.CreatorBoxed();
                        }
                        var addMethod = typeDetail.GetMethodBoxed("Add", [innerType]);
                        var addMethodArgs = new object?[1];
                        foreach (var item in valueArray!)
                        {
                            var value = item.Bind(innerType);
                            addMethodArgs[0] = value;
                            addMethod.CallerBoxed(set, addMethodArgs);
                        }
                        return set;
                    }
                    else if (typeDetail.Type.IsArray || typeDetail.Type.IsInterface)
                    {
                        var array = Array.CreateInstance(innerType, valueArray!.Count);
                        for (var i = 0; i < valueArray.Count; i++)
                        {
                            var value = valueArray[i].Bind(innerType);
                            array.SetValue(value, i);
                        }
                        return array;
                    }
                    else
                    {
                        throw new InvalidCastException();
                    }
                }
            }

            if (jsonType != JsonObjectType.Object)
                throw new InvalidCastException();
            var obj = typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
            foreach (var item in valueProperties!)
            {
                if (typeDetail.TryGetSerializableMemberCaseInsensitive(item.Key, out var member))
                {
                    var value = item.Value.Bind(member.Type);
                    if (value is not null && obj is not null)
                        member.SetterBoxed(obj, value);
                }
            }
            return obj;
        }
    }
}
