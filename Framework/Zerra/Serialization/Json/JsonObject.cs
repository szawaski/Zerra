// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Globalization;
using System.Text;

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Represents a JSON value that can be of various types including null, boolean, number, string, object, or array.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonObject"/> provides a flexible way to work with JSON data without requiring strong typing.
    /// It supports implicit conversion to and from various primitive types and can be used to navigate JSON structures dynamically.
    /// Use explicit casting operators to convert <see cref="JsonObject"/> instances to specific types.
    /// </remarks>
    public class JsonObject : IEnumerable
    {
        //invalid UTF8 surrogate characters
        private const char lowerSurrogate = (char)55296; //D800
        private const char upperSurrogate = (char)57343; //DFFF

        private readonly JsonObjectType jsonType;
        private readonly bool valueBoolean;
        private readonly decimal valueNumber;
        private readonly string? valueString;
        private readonly Dictionary<string, JsonObject>? valueProperties;
        private readonly List<JsonObject>? valueArray;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with a null value.
        /// </summary>
        public JsonObject()
        {
            jsonType = JsonObjectType.Null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with a boolean value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        public JsonObject(bool value)
        {
            jsonType = JsonObjectType.Boolean;
            valueBoolean = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with a numeric value.
        /// </summary>
        /// <param name="value">The numeric value.</param>
        public JsonObject(decimal value)
        {
            jsonType = JsonObjectType.Number;
            valueNumber = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with a string value.
        /// </summary>
        /// <param name="value">The string value.</param>
        public JsonObject(string value)
        {
            jsonType = JsonObjectType.String;
            valueString = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with an object (dictionary) value.
        /// </summary>
        /// <param name="value">A dictionary representing the JSON object properties.</param>
        public JsonObject(Dictionary<string, JsonObject> value)
        {
            jsonType = JsonObjectType.Object;
            valueProperties = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class with an array value.
        /// </summary>
        /// <param name="value">A list of <see cref="JsonObject"/> instances representing the JSON array elements.</param>
        public JsonObject(List<JsonObject> value)
        {
            jsonType = JsonObjectType.Array;
            valueArray = value;
        }

        /// <summary>
        /// Represents the different types a <see cref="JsonObject"/> can hold.
        /// </summary>
        public enum JsonObjectType
        {
            /// <summary>Represents a null JSON value.</summary>
            Null,
            /// <summary>Represents a boolean JSON value.</summary>
            Boolean,
            /// <summary>Represents a numeric JSON value.</summary>
            Number,
            /// <summary>Represents a string JSON value.</summary>
            String,
            /// <summary>Represents a JSON object with properties.</summary>
            Object,
            /// <summary>Represents a JSON array of values.</summary>
            Array
        }

        /// <summary>
        /// Gets the type of this JSON object.
        /// </summary>
        public JsonObjectType JsonType => jsonType;

        /// <summary>
        /// Gets a value indicating whether this JSON object is null.
        /// </summary>
        public bool IsNull => jsonType == JsonObjectType.Null;

        /// <summary>
        /// Gets or sets a property value in this JSON object.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <returns>The <see cref="JsonObject"/> value associated with the property.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Object"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if the property does not exist.</exception>
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

        /// <summary>
        /// Gets or sets an element in this JSON array by index.
        /// </summary>
        /// <param name="index">The zero-based index of the element.</param>
        /// <returns>The <see cref="JsonObject"/> value at the specified index.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/>.</exception>
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

        /// <summary>
        /// Adds an element to this JSON array.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> to add.</param>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/>.</exception>
        public void Add(JsonObject jsonObject)
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            valueArray!.Add(jsonObject);
        }

        /// <summary>
        /// Removes an element from this JSON array.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> to remove.</param>
        /// <returns><c>true</c> if the element was found and removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/>.</exception>
        public bool Remove(JsonObject jsonObject)
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.Remove(jsonObject);
        }

        /// <summary>
        /// Returns a JSON-formatted string representation of this object.
        /// </summary>
        /// <returns>A JSON string representation.</returns>
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
                            _ = sb.Append(':');
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
                        {
                            if (c >= lowerSurrogate && c <= upperSurrogate)
                            {
#if NETSTANDARD2_0
                                _ = sb.Append(chars.Slice(start, i - start).ToArray());
#else
                                _ = sb.Append(chars.Slice(start, i - start));
#endif
                                start = i + 1;
                                var surrogateCode = StringHelper.SurrogateIntToEncodedHexChars[c];
                                _ = sb.Append(surrogateCode);
                                continue;
                            }
                            continue;
                        }


#if NETSTANDARD2_0
                        _ = sb.Append(chars.Slice(start, i - start).ToArray());
#else
                        _ = sb.Append(chars.Slice(start, i - start));
#endif
                        start = i + 1;
                        var code = StringHelper.LowUnicodeIntToEncodedHexChars[c];
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

        /// <summary>
        /// Converts this JSON object to a boolean value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The boolean value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Boolean"/>.</exception>
        public static explicit operator bool(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Boolean)
                throw new InvalidCastException();
            return obj.valueBoolean;
        }

        /// <summary>
        /// Converts this JSON object to a byte value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The byte value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator byte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (byte)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a sbyte value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The sbyte value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator sbyte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (sbyte)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a short value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The short value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator short(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (short)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a ushort value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The ushort value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator ushort(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ushort)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to an int value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The int value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator int(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (int)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a uint value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The uint value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator uint(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (uint)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a long value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The long value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator long(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (long)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a ulong value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The ulong value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator ulong(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ulong)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a float value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The float value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator float(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (float)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a double value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The double value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator double(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (double)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a decimal value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The decimal value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/>.</exception>
        public static explicit operator decimal(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a char value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The first character of the string value.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or the string is empty.</exception>
        public static explicit operator char(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            if (obj.valueString!.Length == 0)
                throw new InvalidCastException();
            return obj.valueString[0];
        }

        /// <summary>
        /// Converts this JSON object to a DateTime value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateTime value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator DateTime(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTime.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Converts this JSON object to a DateTimeOffset value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateTimeOffset value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator DateTimeOffset(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTimeOffset.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Converts this JSON object to a TimeSpan value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The TimeSpan value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator TimeSpan(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeSpan.Parse(obj.valueString!);
        }
#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts this JSON object to a DateOnly value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateOnly value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator DateOnly(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateOnly.Parse(obj.valueString!);
        }

        /// <summary>
        /// Converts this JSON object to a TimeOnly value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The TimeOnly value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator TimeOnly(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeOnly.Parse(obj.valueString!);
        }
#endif

        /// <summary>
        /// Converts this JSON object to a Guid value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The Guid value parsed from the string representation.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator Guid(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return Guid.Parse(obj.valueString!);
        }

        /// <summary>
        /// Converts this JSON object to a string value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The string value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>.</exception>
        public static explicit operator string?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return obj.valueString;
        }

        /// <summary>
        /// Converts this JSON object to a nullable boolean value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The boolean value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Boolean"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator bool?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Boolean)
                throw new InvalidCastException();
            return obj.valueBoolean;
        }

        /// <summary>
        /// Converts this JSON object to a nullable byte value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The byte value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator byte?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (byte)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable sbyte value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The sbyte value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator sbyte?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (sbyte)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable short value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The short value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator short?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (short)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable ushort value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The ushort value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator ushort?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ushort)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable int value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The int value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator int?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (int)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable uint value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The uint value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator uint?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (uint)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable long value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The long value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator long?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (long)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable ulong value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The ulong value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator ulong?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (ulong)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable float value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The float value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator float?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (float)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable double value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The double value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator double?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (double)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable decimal value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The decimal value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Number"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator decimal?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.Number)
                throw new InvalidCastException();
            return (decimal)obj.valueNumber;
        }

        /// <summary>
        /// Converts this JSON object to a nullable char value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The first character of the string value, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/>, <see cref="JsonObjectType.Null"/>, or the string is empty.</exception>
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

        /// <summary>
        /// Converts this JSON object to a nullable DateTime value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateTime value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator DateTime?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTime.Parse(obj.valueString!, null, DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Converts this JSON object to a nullable DateTimeOffset value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateTimeOffset value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator DateTimeOffset?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateTimeOffset.Parse(obj.valueString!);
        }

        /// <summary>
        /// Converts this JSON object to a nullable TimeSpan value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The TimeSpan value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator TimeSpan?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeSpan.Parse(obj.valueString!);
        }
#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts this JSON object to a nullable DateOnly value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The DateOnly value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator DateOnly?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return DateOnly.Parse(obj.valueString!);
        }

        /// <summary>
        /// Converts this JSON object to a nullable TimeOnly value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The TimeOnly value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator TimeOnly?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return TimeOnly.Parse(obj.valueString!);
        }
#endif

        /// <summary>
        /// Converts this JSON object to a nullable Guid value.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>The Guid value parsed from the string representation, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.String"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator Guid?(JsonObject obj)
        {
            if (obj.jsonType == JsonObjectType.Null)
                return null;
            if (obj.jsonType != JsonObjectType.String)
                throw new InvalidCastException();
            return Guid.Parse(obj.valueString!);
        }

        /// <summary>
        /// Converts this JSON object to a JSON object array.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>An array of <see cref="JsonObject"/> instances, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator JsonObject[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return obj.valueArray!.ToArray();
        }

        /// <summary>
        /// Converts this JSON object to a list of JSON objects.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to convert.</param>
        /// <returns>A list of <see cref="JsonObject"/> instances, or <c>null</c> if the object is null.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/> or <see cref="JsonObjectType.Null"/>.</exception>
        public static explicit operator List<JsonObject>?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return obj.valueArray!;
        }

        /// <summary>
        /// Returns an enumerator for iterating through the elements of this JSON array.
        /// </summary>
        /// <returns>An enumerator for the array elements.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/>.</exception>
        public IEnumerator<JsonObject> GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return ((IEnumerable<JsonObject>)valueArray!).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator for iterating through the elements of this JSON array.
        /// </summary>
        /// <returns>An enumerator for the array elements.</returns>
        /// <exception cref="InvalidCastException">Thrown if this object is not of type <see cref="JsonObjectType.Array"/>.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.GetEnumerator();
        }

    }
}
