// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public class JsonObject : IEnumerable
    {
        private readonly JsonObjectType jsonType;
        private readonly string? valueString;
        private readonly Dictionary<string, JsonObject>? valueProperties;
        private readonly JsonObject[]? valueArray;

        public JsonObjectType JsonType => jsonType;

        public JsonObject(string? value, bool literal)
        {
            jsonType = literal ? JsonObjectType.Literal : JsonObjectType.String;
            valueString = value;
            valueProperties = null;
            valueArray = null;
        }

        public JsonObject(Dictionary<string, JsonObject> value)
        {
            jsonType = JsonObjectType.Object;
            valueString = null;
            valueProperties = value;
            valueArray = null;
        }

        public JsonObject(JsonObject[] value)
        {
            jsonType = JsonObjectType.Array;
            valueString = null;
            valueProperties = null;
            valueArray = value;
        }

        public enum JsonObjectType
        {
            Literal,
            String,
            Object,
            Array
        }

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
        }

        public bool IsNull
        {
            get
            {
                return jsonType == JsonObjectType.Literal && valueString == null;
            }
        }

        public override string ToString()
        {
            var writer = new CharWriter();
            try
            {
                ToString(ref writer);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        private void ToString(ref CharWriter writer)
        {
            switch (jsonType)
            {
                case JsonObjectType.Literal:
                    {
                        if (valueString == null)
                            writer.Write("null");
                        else
                            writer.Write(valueString);
                    }
                    break;
                case JsonObjectType.String:
                    {
                        JsonSerializer.ToStringJsonString(valueString, ref writer);
                    }
                    break;
                case JsonObjectType.Object:
                    {
                        writer.Write('{');
                        var first = true;
                        foreach (var item in valueProperties!)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            JsonSerializer.ToStringJsonString(item.Key, ref writer);
                            writer.Write(':');
                            item.Value.ToString(ref writer);
                        }
                        writer.Write('}');
                    }
                    break;
                case JsonObjectType.Array:
                    {
                        writer.Write('[');
                        var first = true;
                        foreach (var item in valueArray!)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            item.ToString(ref writer);
                        }
                        writer.Write(']');
                    }
                    break;
            }
        }

        public static explicit operator bool(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return bool.Parse(obj.valueString);
        }
        public static explicit operator byte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return byte.Parse(obj.valueString);
        }
        public static explicit operator sbyte(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return sbyte.Parse(obj.valueString);
        }
        public static explicit operator short(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return short.Parse(obj.valueString);
        }
        public static explicit operator ushort(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return ushort.Parse(obj.valueString);
        }
        public static explicit operator int(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return int.Parse(obj.valueString);
        }
        public static explicit operator uint(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return uint.Parse(obj.valueString);
        }
        public static explicit operator long(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return long.Parse(obj.valueString);
        }
        public static explicit operator ulong(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return ulong.Parse(obj.valueString);
        }
        public static explicit operator float(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return float.Parse(obj.valueString);
        }
        public static explicit operator double(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return double.Parse(obj.valueString);
        }
        public static explicit operator decimal(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return decimal.Parse(obj.valueString);
        }
        public static explicit operator char(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null || obj.valueString.Length == 0)
                throw new InvalidCastException();
            return obj.valueString[0];
        }
        public static explicit operator DateTime(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return DateTime.Parse(obj.valueString, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator DateTimeOffset(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.IsNull || obj.valueString == null)
                throw new InvalidCastException();
            return DateTimeOffset.Parse(obj.valueString, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator TimeSpan(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null)
                throw new InvalidCastException();
            return TimeSpan.Parse(obj.valueString!);
        }
        public static explicit operator Guid(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null)
                throw new InvalidCastException();
            return Guid.Parse(obj.valueString);
        }

        public static explicit operator string?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null)
                return null;
            return obj.valueString;
        }

        public static explicit operator bool?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return bool.Parse(obj.valueString);
        }
        public static explicit operator byte?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return byte.Parse(obj.valueString);
        }
        public static explicit operator sbyte?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return sbyte.Parse(obj.valueString);
        }
        public static explicit operator short?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return short.Parse(obj.valueString);
        }
        public static explicit operator ushort?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return ushort.Parse(obj.valueString);
        }
        public static explicit operator int?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return int.Parse(obj.valueString);
        }
        public static explicit operator uint?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return uint.Parse(obj.valueString);
        }
        public static explicit operator long?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return long.Parse(obj.valueString);
        }
        public static explicit operator ulong?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return ulong.Parse(obj.valueString);
        }
        public static explicit operator float?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return float.Parse(obj.valueString);
        }
        public static explicit operator double?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return double.Parse(obj.valueString);
        }
        public static explicit operator decimal?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return decimal.Parse(obj.valueString);
        }
        public static explicit operator char?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return obj.valueString[0];
        }
        public static explicit operator DateTime?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return DateTime.Parse(obj.valueString, null, DateTimeStyles.RoundtripKind);
        }
        public static explicit operator DateTimeOffset?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return DateTimeOffset.Parse(obj.valueString);
        }
        public static explicit operator TimeSpan?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return TimeSpan.Parse(obj.valueString);
        }
#if NET6_0_OR_GREATER
        public static explicit operator DateOnly?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return DateOnly.Parse(obj.valueString);
        }
        public static explicit operator TimeOnly?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return TimeOnly.Parse(obj.valueString);
        }
#endif
        public static explicit operator Guid?(JsonObject obj)
        {
            if (obj.jsonType != JsonObjectType.String && obj.jsonType != JsonObjectType.Literal)
                throw new InvalidCastException();
            if (obj.valueString == null || obj.valueString == string.Empty)
                return null;
            return Guid.Parse(obj.valueString);
        }

        public static explicit operator bool[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new bool[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (bool)obj.valueArray[i];
            return array;
        }
        public static explicit operator byte[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;

            //special case
            if (obj.jsonType == JsonObjectType.String || obj.jsonType == JsonObjectType.Literal)
                return Convert.FromBase64String(obj.valueString!);

            var array = new byte[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (byte)obj.valueArray[i];
            return array;
        }
        public static explicit operator sbyte[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;

            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new sbyte[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (sbyte)obj.valueArray[i];
            return array;
        }
        public static explicit operator short[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new short[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (short)obj.valueArray[i];
            return array;
        }
        public static explicit operator ushort[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new ushort[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (ushort)obj.valueArray[i];
            return array;
        }
        public static explicit operator int[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new int[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (int)obj.valueArray[i];
            return array;
        }
        public static explicit operator uint[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new uint[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (uint)obj.valueArray[i];
            return array;
        }
        public static explicit operator long[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new long[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (long)obj.valueArray[i];
            return array;
        }
        public static explicit operator ulong[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new ulong[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (ulong)obj.valueArray[i];
            return array;
        }
        public static explicit operator float[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new float[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (float)obj.valueArray[i];
            return array;
        }
        public static explicit operator double[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new double[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (double)obj.valueArray[i];
            return array;
        }
        public static explicit operator decimal[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new decimal[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (decimal)obj.valueArray[i];
            return array;
        }
        public static explicit operator char[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new char[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (char)obj.valueArray[i];
            return array;
        }
        public static explicit operator DateTime[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateTime[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateTime)obj.valueArray[i];
            return array;
        }
        public static explicit operator DateTimeOffset[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateTimeOffset[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateTimeOffset)obj.valueArray[i];
            return array;
        }
        public static explicit operator TimeSpan[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new TimeSpan[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (TimeSpan)obj.valueArray[i];
            return array;
        }
#if NET6_0_OR_GREATER
        public static explicit operator DateOnly[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateOnly[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateOnly)obj.valueArray[i];
            return array;
        }
        public static explicit operator TimeOnly[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new TimeOnly[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (TimeOnly)obj.valueArray[i];
            return array;
        }
#endif
        public static explicit operator Guid[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new Guid[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (Guid)obj.valueArray[i];
            return array;
        }

        public static explicit operator string?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new string?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (string?)obj.valueArray[i];
            return array;
        }

        public static explicit operator bool?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new bool?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (bool?)obj.valueArray[i];
            return array;
        }
        public static explicit operator byte?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new byte?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (byte?)obj.valueArray[i];
            return array;
        }
        public static explicit operator sbyte?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new sbyte?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (sbyte?)obj.valueArray[i];
            return array;
        }
        public static explicit operator short?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new short?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (short?)obj.valueArray[i];
            return array;
        }
        public static explicit operator ushort?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new ushort?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (ushort?)obj.valueArray[i];
            return array;
        }
        public static explicit operator int?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new int?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (int?)obj.valueArray[i];
            return array;
        }
        public static explicit operator uint?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new uint?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (uint?)obj.valueArray[i];
            return array;
        }
        public static explicit operator long?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new long?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (long?)obj.valueArray[i];
            return array;
        }
        public static explicit operator ulong?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new ulong?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (ulong?)obj.valueArray[i];
            return array;
        }
        public static explicit operator float?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new float?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (float?)obj.valueArray[i];
            return array;
        }
        public static explicit operator double?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new double?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (double?)obj.valueArray[i];
            return array;
        }
        public static explicit operator decimal?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new decimal?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (decimal?)obj.valueArray[i];
            return array;
        }
        public static explicit operator char?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new char?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (char?)obj.valueArray[i];
            return array;
        }
        public static explicit operator DateTime?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateTime?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateTime?)obj.valueArray[i];
            return array;
        }
        public static explicit operator DateTimeOffset?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateTimeOffset?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateTimeOffset?)obj.valueArray[i];
            return array;
        }
        public static explicit operator TimeSpan?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new TimeSpan?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (TimeSpan?)obj.valueArray[i];
            return array;
        }
#if NET6_0_OR_GREATER
        public static explicit operator DateOnly?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new DateOnly?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (DateOnly?)obj.valueArray[i];
            return array;
        }
        public static explicit operator TimeOnly?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new TimeOnly?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (TimeOnly?)obj.valueArray[i];
            return array;
        }
#endif
        public static explicit operator Guid?[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            var array = new Guid?[obj.valueArray!.Length];
            for (var i = 0; i < array.Length; i++)
                array[i] = (Guid?)obj.valueArray[i];
            return array;
        }

        public static explicit operator JsonObject[]?(JsonObject obj)
        {
            if (obj.IsNull)
                return null;
            if (obj.jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return obj.valueArray;
        }

        public IEnumerator<JsonObject> GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (jsonType != JsonObjectType.Array)
                throw new InvalidCastException();
            return valueArray!.GetEnumerator();
        }

        private static readonly Type plainListType = typeof(List<>);
        public T? Bind<T>() { return (T?)Bind(typeof(T)); }
        public object? Bind(Type type)
        {
            if (IsNull)
                return null;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (typeDetail.HasIEnumerableGeneric)
            {
                if (typeDetail.HasIDictionaryGeneric || typeDetail.HasIReadOnlyDictionaryGeneric)
                {
                    if (jsonType != JsonObjectType.Object)
                        throw new InvalidCastException();
                    IDictionary dictionary;
                    if (typeDetail.Type.IsInterface)
                    {
                        var dictionaryType = TypeAnalyzer.GetGenericType(typeof(Dictionary<,>), (Type[])typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                        dictionary = (IDictionary)Instantiator.Create(dictionaryType);
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
                else if (typeDetail.Type == typeof(byte[]))
                {
                    if (jsonType != JsonObjectType.String)
                        throw new InvalidCastException();
                    if (valueString == null)
                        return null;
                    return Convert.FromBase64String(valueString);
                }
                else
                {
                    if (jsonType != JsonObjectType.Array)
                        throw new InvalidCastException();
                    var innerType = typeDetail.InnerTypeDetails[0].Type;
                    if (!typeDetail.Type.IsArray && typeDetail.HasIListGeneric)
                    {
                        var listType = TypeAnalyzer.GetGenericTypeDetail(plainListType, innerType);
                        var list = (IList)listType.CreatorBoxed();
                        foreach (var item in valueArray!)
                        {
                            var value = item.Bind(innerType);
                            _ = list.Add(value);
                        }
                        return list;
                    }
                    else
                    {
                        var array = Array.CreateInstance(innerType, valueArray!.Length);
                        for (var i = 0; i < valueArray.Length; i++)
                        {
                            var value = valueArray[i].Bind(innerType);
                            array.SetValue(value, i);
                        }
                        return array;
                    }
                }
            }

            if (typeDetail.CoreType.HasValue)
            {
                if (jsonType != JsonObjectType.String && jsonType != JsonObjectType.Literal)
                    throw new InvalidCastException();
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
                return Enum.Parse(type, valueString!);
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].Type.IsEnum)
            {
                if (valueString == null)
                    return null;
                return Enum.Parse(typeDetail.InnerTypeDetails[0].Type, valueString);
            }

            if (jsonType != JsonObjectType.Object)
                throw new InvalidCastException();
            var obj = typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
            foreach (var item in valueProperties!)
            {
                if (typeDetail.TryGetSerializableMemberCaseInsensitive(item.Key, out var member))
                {
                    var value = item.Value.Bind(member.Type);
                    if (value != null && obj != null)
                        member.SetterBoxed(obj, value);
                }
            }
            return obj;
        }
    }
}
