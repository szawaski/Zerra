// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private static readonly Type genericListType = typeof(List<>);
        private static readonly Type genericHashSetType = typeof(HashSet<>);

        public static T Deserialize<T>(string json, Graph graph = null) { return Deserialize<T>(json.AsSpan(), graph); }
        public static object Deserialize(string json, Type type, Graph graph = null) { return Deserialize(json.AsSpan(), type, graph); }

        public static T Deserialize<T>(ReadOnlySpan<char> json, Graph graph = null)
        {
            var type = typeof(T);
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (json == null || json.Length == 0)
                return (T)ConvertStringToType(String.Empty, typeDetails);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, false);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return (T)value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }
        public static object Deserialize(ReadOnlySpan<char> json, Type type, Graph graph = null)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (json == null || json.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, false);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static T Deserialize<T>(byte[] bytes, Graph graph = null) { return Deserialize<T>(bytes.AsSpan(), graph); }
        public static object Deserialize(byte[] bytes, Type type, Graph graph = null) { return Deserialize(bytes.AsSpan(), type, graph); }

        public static T Deserialize<T>(ReadOnlySpan<byte> bytes, Graph graph = null)
        {
            var obj = Deserialize(bytes, typeof(T), graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object Deserialize(ReadOnlySpan<byte> bytes, Type type, Graph graph = null)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (bytes == null || bytes.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);


#if NETSTANDARD2_0
            Span<char> chars = Encoding.UTF8.GetChars(bytes.ToArray(), 0, bytes.Length).AsSpan();
#else
            Span<char> chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
            var count = Encoding.UTF8.GetChars(bytes, chars);
            chars = chars.Slice(0, count);
#endif

            var reader = new CharReader(chars);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, false);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static T Deserialize<T>(Stream stream, Graph graph = null)
        {
            var obj = Deserialize(stream, typeof(T), graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object Deserialize(Stream stream, Type type, Graph graph = null)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            var reader = new CharReader(stream, new UTF8Encoding(false));
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, false);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static T DeserializeNameless<T>(string json, Graph graph = null) { return DeserializeNameless<T>(json.AsSpan(), graph); }
        public static object DeserializeNameless(string json, Type type, Graph graph) { return DeserializeNameless(json.AsSpan(), type, graph); }

        public static T DeserializeNameless<T>(ReadOnlySpan<char> json, Graph graph = null)
        {
            var obj = DeserializeNameless(json, typeof(T), graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object DeserializeNameless(ReadOnlySpan<char> json, Type type, Graph graph)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (json == null || json.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, true);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static T DeserializeNameless<T>(byte[] bytes, Graph graph = null) { return DeserializeNameless<T>(bytes.AsSpan(), graph); }
        public static object DeserializeNameless(byte[] bytes, Type type, Graph graph) { return DeserializeNameless(bytes.AsSpan(), type, graph); }

        public static T DeserializeNameless<T>(ReadOnlySpan<byte> bytes, Graph graph = null)
        {
            var obj = DeserializeNameless(bytes, typeof(T), graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object DeserializeNameless(ReadOnlySpan<byte> bytes, Type type, Graph graph)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (bytes.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

            if (bytes == null || bytes.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

#if NETSTANDARD2_0
            Span<char> chars = Encoding.UTF8.GetChars(bytes.ToArray(), 0, bytes.Length).AsSpan();
#else
            Span<char> chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
            var count = Encoding.UTF8.GetChars(bytes, chars);
            chars = chars.Slice(0, count);
#endif

            var reader = new CharReader(chars);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, true);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static T DeserializeNameless<T>(Stream stream, Graph graph = null)
        {
            var obj = DeserializeNameless(stream, typeof(T), graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object DeserializeNameless(Stream stream, Type type, Graph graph = null)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            var reader = new CharReader(stream, new UTF8Encoding(false));
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetails, graph, true);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        public static JsonObject DeserializeJsonObject(string json, Graph graph = null)
        {
            if (json.Length == 0)
                return new JsonObject(null, true);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriteBuffer();
            try
            {
                if (!reader.ReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
                if (reader.HasMoreChars())
                    throw reader.CreateException("Unexpected character");
                return value;
            }
            finally
            {
                reader.Dispose();
                decodeBuffer.Dispose();
            }
        }

        private static object FromJson(char c, ref CharReader reader, ref CharWriteBuffer decodeBuffer, TypeDetail typeDetail, Graph graph, bool nameless)
        {
            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = TypeAnalyzer.GetTypeDetail(emptyImplementationType);
            }

            switch (c)
            {
                case '"':
                    var value = ReadString(ref reader, ref decodeBuffer);
                    return ConvertStringToType(value, typeDetail);
                case '{':
                    return FromJsonObject(ref reader, ref decodeBuffer, typeDetail, graph, nameless);
                case '[':
                    if (!nameless || (typeDetail != null && typeDetail.IsIEnumerableGeneric))
                        return FromJsonArray(ref reader, ref decodeBuffer, typeDetail, graph, nameless);
                    else
                        return FromJsonArrayNameless(ref reader, ref decodeBuffer, typeDetail, graph);
                default:
                    return FromLiteral(c, ref reader, typeDetail);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromJsonObject(ref CharReader reader, ref CharWriteBuffer decodeBuffer, TypeDetail typeDetail, Graph graph, bool nameless)
        {
            var obj = typeDetail?.Creator();
            var canExpectComma = false;
            while (reader.ReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var propertyName = ReadString(ref reader, ref decodeBuffer);

                        ReadPropertySperator(ref reader);

                        if (!reader.ReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        if (obj != null)
                        {
                            if (typeDetail != null && typeDetail.SpecialType == SpecialType.Dictionary)
                            {
                                //Dictionary Special Case
                                var value = FromJson(c, ref reader, ref decodeBuffer, typeDetail.InnerTypeDetails[0].InnerTypeDetails[1], null, nameless);
                                if (typeDetail.InnerTypeDetails[0].InnerTypeDetails[0].CoreType.HasValue)
                                {
                                    var key = TypeAnalyzer.Convert(propertyName, typeDetail.InnerTypeDetails[0].InnerTypes[0]);
                                    var method = typeDetail.GetMethod("Add");
                                    _ = method.Caller(obj, new object[] { key, value });
                                }
                            }
                            else
                            {
                                if (typeDetail.TryGetSerializableMemberDetails(propertyName, out MemberDetail memberDetail))
                                {
                                    var propertyGraph = graph?.GetChildGraph(memberDetail.Name);
                                    var value = FromJson(c, ref reader, ref decodeBuffer, memberDetail.TypeDetail, propertyGraph, nameless);
                                    if (value != null)
                                    {
                                        if (graph != null)
                                        {
                                            if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                            {
                                                if (graph.HasLocalProperty(memberDetail.Name))
                                                    memberDetail.Setter(obj, value);
                                            }
                                            else
                                            {
                                                if (propertyGraph != null)
                                                    memberDetail.Setter(obj, value);
                                            }
                                        }
                                        else
                                        {
                                            memberDetail.Setter(obj, value);
                                        }
                                    }
                                }
                                else
                                {
                                    _ = FromJson(c, ref reader, ref decodeBuffer, null, null, nameless);
                                }
                            }
                        }
                        else
                        {
                            _ = FromJson(c, ref reader, ref decodeBuffer, null, null, nameless);
                        }
                        canExpectComma = true;
                        break;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    case '}':
                        return obj;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromJsonArray(ref CharReader reader, ref CharWriteBuffer decodeBuffer, TypeDetail typeDetail, Graph graph, bool nameless)
        {
            object collection = null;
            MethodDetail addMethod = null;
            object[] addMethodArgs = null;
            TypeDetail arrayElementType = null;
            if (typeDetail != null && typeDetail.IsIEnumerableGeneric)
            {
                arrayElementType = typeDetail.IEnumerableGenericInnerTypeDetails;
                if (typeDetail.Type.IsArray)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                    collection = genericListType.Creator();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsIList && typeDetail.Type.IsInterface)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                    collection = genericListType.Creator();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsIList && !typeDetail.Type.IsInterface)
                {
                    collection = typeDetail.Creator();
                    addMethod = typeDetail.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsISet && typeDetail.Type.IsInterface)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericHashSetType, typeDetail.InnerTypeDetails[0].Type));
                    collection = genericListType.Creator();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsISet && !typeDetail.Type.IsInterface)
                {
                    collection = typeDetail.Creator();
                    addMethod = typeDetail.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                    collection = genericListType.Creator();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
            }

            bool canExpectComma = false;
            while (reader.ReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        if (collection == null)
                            return null;
                        if (typeDetail.Type.IsArray && arrayElementType != null)
                        {
                            var list = (IList)collection;
                            var array = Array.CreateInstance(arrayElementType.Type, list.Count);
                            for (int i = 0; i < list.Count; i++)
                                array.SetValue(list[i], i);
                            return array;
                        }
                        return collection;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    default:
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var value = FromJson(c, ref reader, ref decodeBuffer, arrayElementType, graph, nameless);
                        if (collection != null)
                        {
                            addMethodArgs[0] = value;
                            _ = addMethod.Caller(collection, addMethodArgs);
                        }
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromJsonArrayNameless(ref CharReader reader, ref CharWriteBuffer decodeBuffer, TypeDetail typeDetail, Graph graph)
        {
            var obj = typeDetail?.Creator();
            bool canExpectComma = false;
            int propertyIndexForNameless = 0;
            while (reader.ReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        return obj;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    default:
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        if (obj != null)
                        {
                            var memberDetail = typeDetail != null && propertyIndexForNameless < typeDetail.SerializableMemberDetails.Count
                                ? typeDetail.SerializableMemberDetails[propertyIndexForNameless++]
                                : null;
                            if (memberDetail != null)
                            {
                                var propertyGraph = graph?.GetChildGraph(memberDetail.Name);
                                var value = FromJson(c, ref reader, ref decodeBuffer, memberDetail?.TypeDetail, propertyGraph, true);
                                if (memberDetail != null && memberDetail.TypeDetail.SpecialType.HasValue && memberDetail.TypeDetail.SpecialType == SpecialType.Dictionary)
                                {
                                    var dictionary = memberDetail.TypeDetail.Creator();
                                    var addMethod = memberDetail.TypeDetail.GetMethod("Add");
                                    var keyGetter = memberDetail.TypeDetail.InnerTypeDetails[0].GetMember("Key").Getter;
                                    var valueGetter = memberDetail.TypeDetail.InnerTypeDetails[0].GetMember("Value").Getter;
                                    foreach (var item in (IEnumerable)value)
                                    {
                                        var itemKey = keyGetter(item);
                                        var itemValue = valueGetter(item);
                                        _ = addMethod.Caller(dictionary, new object[] { itemKey, itemValue });
                                    }
                                    memberDetail.Setter(obj, dictionary);
                                }
                                else
                                {
                                    if (value != null)
                                    {
                                        if (graph != null)
                                        {
                                            if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                            {
                                                if (graph.HasLocalProperty(memberDetail.Name))
                                                    memberDetail.Setter(obj, value);
                                            }
                                            else
                                            {
                                                if (propertyGraph != null)
                                                    memberDetail.Setter(obj, value);
                                            }
                                        }
                                        else
                                        {
                                            memberDetail.Setter(obj, value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _ = FromJson(c, ref reader, ref decodeBuffer, null, null, true);
                            }
                        }
                        else
                        {
                            _ = FromJson(c, ref reader, ref decodeBuffer, null, null, true);
                        }
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object FromLiteral(char c, ref CharReader reader, TypeDetail typeDetail)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'u') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertNullToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 't':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'r') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'u') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'e') throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertTrueToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 'f':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'a') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 's') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'e') throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertFalseToType(typeDetail.CoreType.Value);
                        return null;
                    }
                default:
                    {
                        if (typeDetail != null && typeDetail.CoreType == CoreType.String)
                        {
                            var value = ReadLiteralNumberAsString(c, ref reader);
                            return value;
                        }
                        else
                        {
                            if (typeDetail != null && typeDetail.CoreType.HasValue)
                                return ReadLiteralNumberAsType(c, typeDetail.CoreType.Value, ref reader);
                            ReadLiteralNumberAsEmpty(c, ref reader);
                            return null;
                        }
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertStringToType(string s, TypeDetail typeDetail)
        {
            if (typeDetail == null)
                return null;

            if (typeDetail.CoreType.HasValue)
            {
                switch (typeDetail.CoreType.Value)
                {
                    case CoreType.String:
                        {
                            return s;
                        }
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Boolean.TryParse(s, out bool value))
                                return value;
                            return null;
                        }
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Byte.TryParse(s, out byte value))
                                return value;
                            return null;
                        }
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (SByte.TryParse(s, out sbyte value))
                                return value;
                            return null;
                        }
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int16.TryParse(s, out short value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt16.TryParse(s, out ushort value))
                                return value;
                            return null;
                        }
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int32.TryParse(s, out int value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt32.TryParse(s, out uint value))
                                return value;
                            return null;
                        }
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int64.TryParse(s, out long value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt64.TryParse(s, out ulong value))
                                return value;
                            return null;
                        }
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Single.TryParse(s, out float value))
                                return value;
                            return null;
                        }
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Double.TryParse(s, out double value))
                                return value;
                            return null;
                        }
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Decimal.TryParse(s, out decimal value))
                                return value;
                            return null;
                        }

                    case CoreType.Char:
                    case CoreType.CharNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            return s[0];
                        }
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (DateTime.TryParse(s, out DateTime value))
                                return value;
                            return null;
                        }
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (DateTimeOffset.TryParse(s, out DateTimeOffset value))
                                return value;
                            return null;
                        }
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (TimeSpan.TryParse(s, out TimeSpan value))
                                return value;
                            return null;
                        }
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Guid.TryParse(s, out Guid value))
                                return value;
                            return null;
                        }
                }
            }

            if (typeDetail.Type.IsEnum)
            {
                var valueString = s.ToString();
                if (EnumName.TryParse(valueString, typeDetail.Type, out object value))
                    return value;
                return null;
            }

            if (typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].Type.IsEnum)
            {
                var valueString = s.ToString();
                if (EnumName.TryParse(valueString, typeDetail.InnerTypeDetails[0].Type, out object value))
                    return value;
                return null;
            }

            if (typeDetail.Type.IsArray && typeDetail.InnerTypeDetails[0].CoreType == CoreType.Byte)
            {
                //special case
                return Convert.FromBase64String(s);
            }

            return null;
        }

        private static JsonObject FromJsonToJsonObject(char c, ref CharReader reader, ref CharWriteBuffer decodeBuffer, Graph graph)
        {
            switch (c)
            {
                case '"':
                    var value = ReadString(ref reader, ref decodeBuffer);
                    return new JsonObject(value, false);
                case '{':
                    return FromJsonObjectToJsonObject(ref reader, ref decodeBuffer, graph);
                case '[':
                    return FromJsonArrayToJsonObject(ref reader, ref decodeBuffer, graph);
                default:
                    return FromLiteralToJsonObject(c, ref reader);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject FromJsonObjectToJsonObject(ref CharReader reader, ref CharWriteBuffer decodeBuffer, Graph graph)
        {
            var properties = new Dictionary<string, JsonObject>();
            var canExpectComma = false;
            while (reader.ReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var propertyName = ReadString(ref reader, ref decodeBuffer);

                        ReadPropertySperator(ref reader);

                        if (!reader.ReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        var propertyGraph = graph?.GetChildGraph(propertyName);
                        var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, propertyGraph);

                        if (graph != null)
                        {
                            switch (value.JsonType)
                            {
                                case JsonObject.JsonObjectType.Literal:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.String:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.Array:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.Object:
                                    if (graph.HasChildGraph(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                            }
                        }
                        else
                        {
                            properties.Add(propertyName, value);
                        }

                        canExpectComma = true;
                        break;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    case '}':
                        return new JsonObject(properties);
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject FromJsonArrayToJsonObject(ref CharReader reader, ref CharWriteBuffer decodeBuffer, Graph graph)
        {
            var arrayList = new List<JsonObject>();

            bool canExpectComma = false;
            while (reader.ReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        return new JsonObject(arrayList.ToArray());
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    default:
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
                        if (arrayList != null)
                            arrayList.Add(value);
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject FromLiteralToJsonObject(char c, ref CharReader reader)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'u') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject(null, true);
                    }
                case 't':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'r') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'u') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'e') throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("true", true);
                    }
                case 'f':
                    {
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'a') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'l') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 's') throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");
                        if (c != 'e') throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("false", true);
                    }
                default:
                    {
                        var value = ReadLiteralNumberAsString(c, ref reader);
                        return new JsonObject(value, true);
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPropertySperator(ref CharReader reader)
        {
            if (!reader.ReadSkipWhiteSpace(out var c))
                throw reader.CreateException("Json ended prematurely");
            if (c == ':')
                return;
            throw reader.CreateException("Unexpected character");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReadString(ref CharReader reader, ref CharWriteBuffer decodeBuffer)
        {
            //return reader.ReadJsonString(decodeBuffer);

            //Quote already started
            reader.BeginSegment(false);
            while (reader.ReadUntil(out var c, '\"', '\\'))
            {
                switch (c)
                {
                    case '\"':
                        {
                            reader.EndSegmentCopyTo(false, ref decodeBuffer);
                            var s = decodeBuffer.ToString();
                            decodeBuffer.Clear();
                            return s;
                        }
                    case '\\':
                        {
                            reader.EndSegmentCopyTo(false, ref decodeBuffer);

                            if (!reader.Read(out c)) throw reader.CreateException("Json ended prematurely");

                            switch (c)
                            {
                                case 'b':
                                    decodeBuffer.Write('\b');
                                    break;
                                case 't':
                                    decodeBuffer.Write('\t');
                                    break;
                                case 'n':
                                    decodeBuffer.Write('\n');
                                    break;
                                case 'f':
                                    decodeBuffer.Write('\f');
                                    break;
                                case 'r':
                                    decodeBuffer.Write('\r');
                                    break;
                                case 'u':
                                    reader.BeginSegment(false);
                                    if (!reader.Foward(4)) throw reader.CreateException("Json ended prematurely");
                                    var unicodeString = reader.EndSegmentToString(true);
                                    if (!lowUnicodeHexToChar.TryGetValue(unicodeString, out char unicodeChar))
                                        throw reader.CreateException("Incomplete escape sequence");
                                    decodeBuffer.Write(unicodeChar);
                                    break;
                                default:
                                    decodeBuffer.Write(c);
                                    break;
                            }

                            reader.BeginSegment(false);
                        }
                        break;
                }
            }

            throw reader.CreateException("Json ended prematurely");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ReadLiteralNumberAsType(char c, CoreType coreType, ref CharReader reader)
        {
            unchecked
            {
                switch (coreType)
                {
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        return (byte)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        return (sbyte)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        return (short)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        return (ushort)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        return (int)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        return (uint)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        return (long)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        return (ulong)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        return (float)ReadLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        return (double)ReadLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        return (decimal)ReadLiteralNumberAsDecimal(c, ref reader);
                }
            }
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReadLiteralNumberAsString(char c, ref CharReader reader)
        {
            reader.BeginSegment(true);

            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    //nothing
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        //nothing
                        break;
                    case '.':
                        {
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        //nothing
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            switch (c)
                                            {
                                                case '0':
                                                case '1':
                                                case '2':
                                                case '3':
                                                case '4':
                                                case '5':
                                                case '6':
                                                case '7':
                                                case '8':
                                                case '9':
                                                case '-':
                                                case '+':
                                                    //nothing
                                                    break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0':
                                                    case '1':
                                                    case '2':
                                                    case '3':
                                                    case '4':
                                                    case '5':
                                                    case '6':
                                                    case '7':
                                                    case '8':
                                                    case '9':
                                                    case '+':
                                                        //nothing
                                                        break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        return reader.EndSegmentToString(false);
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        return reader.EndSegmentToString(true);
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            return reader.EndSegmentToString(true);
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return reader.EndSegmentToString(false);
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return reader.EndSegmentToString(true);
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            switch (c)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '-':
                                case '+':
                                    //nothing
                                    break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return reader.EndSegmentToString(false);
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return reader.EndSegmentToString(true);
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            return reader.EndSegmentToString(true);
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return reader.EndSegmentToString(false);
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return reader.EndSegmentToString(true);
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            return reader.EndSegmentToString(true);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadLiteralNumberAsInt64(char c, ref CharReader reader)
        {
            bool negative = false;
            long number;

            switch (c)
            {
                case '0': number = 0; break;
                case '1': number = 1; break;
                case '2': number = 2; break;
                case '3': number = 3; break;
                case '4': number = 4; break;
                case '5': number = 5; break;
                case '6': number = 6; break;
                case '7': number = 7; break;
                case '8': number = 8; break;
                case '9': number = 9; break;
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0': number *= 10; break;
                    case '1': number = number * 10 + 1; break;
                    case '2': number = number * 10 + 2; break;
                    case '3': number = number * 10 + 3; break;
                    case '4': number = number * 10 + 4; break;
                    case '5': number = number * 10 + 5; break;
                    case '6': number = number * 10 + 6; break;
                    case '7': number = number * 10 + 7; break;
                    case '8': number = number * 10 + 8; break;
                    case '9': number = number * 10 + 9; break;
                    case '.':
                        {
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            bool negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent) exponent *= -1;
                                            number *= (long)Math.Pow(10, (double)exponent);
                                            if (negative) number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            bool negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent) exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent) exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent) exponent *= -1;
                            number *= (long)Math.Pow(10, (double)exponent);
                            if (negative) number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative) number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative) number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative) number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadLiteralNumberAsUInt64(char c, ref CharReader reader)
        {
            var number = c switch
            {
                '0' => 0UL,
                '1' => 1UL,
                '2' => 2UL,
                '3' => 3UL,
                '4' => 4UL,
                '5' => 5UL,
                '6' => 6UL,
                '7' => 7UL,
                '8' => 8UL,
                '9' => 9UL,
                '-' => 0UL,
                _ => throw reader.CreateException("Unexpected character"),
            };
            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0': number *= 10; break;
                    case '1': number = number * 10 + 1; break;
                    case '2': number = number * 10 + 2; break;
                    case '3': number = number * 10 + 3; break;
                    case '4': number = number * 10 + 4; break;
                    case '5': number = number * 10 + 5; break;
                    case '6': number = number * 10 + 6; break;
                    case '7': number = number * 10 + 7; break;
                    case '8': number = number * 10 + 8; break;
                    case '9': number = number * 10 + 9; break;
                    case '.':
                        {
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            bool negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent) exponent *= -1;
                                            number *= (ulong)Math.Pow(10, (double)exponent);
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            bool negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent) exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent) exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent) exponent *= -1;
                            number *= (ulong)Math.Pow(10, (double)exponent);
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadLiteralNumberAsDouble(char c, ref CharReader reader)
        {
            bool negative = false;
            double number;

            switch (c)
            {
                case '0': number = 0; break;
                case '1': number = 1; break;
                case '2': number = 2; break;
                case '3': number = 3; break;
                case '4': number = 4; break;
                case '5': number = 5; break;
                case '6': number = 6; break;
                case '7': number = 7; break;
                case '8': number = 8; break;
                case '9': number = 9; break;
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0': number *= 10; break;
                    case '1': number = number * 10 + 1; break;
                    case '2': number = number * 10 + 2; break;
                    case '3': number = number * 10 + 3; break;
                    case '4': number = number * 10 + 4; break;
                    case '5': number = number * 10 + 5; break;
                    case '6': number = number * 10 + 6; break;
                    case '7': number = number * 10 + 7; break;
                    case '8': number = number * 10 + 8; break;
                    case '9': number = number * 10 + 9; break;
                    case '.':
                        {
                            double fraction = 10;
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': break;
                                    case '1': number += 1 / fraction; break;
                                    case '2': number += 2 / fraction; break;
                                    case '3': number += 3 / fraction; break;
                                    case '4': number += 4 / fraction; break;
                                    case '5': number += 5 / fraction; break;
                                    case '6': number += 6 / fraction; break;
                                    case '7': number += 7 / fraction; break;
                                    case '8': number += 8 / fraction; break;
                                    case '9': number += 9 / fraction; break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            bool negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent) exponent *= -1;
                                            number *= Math.Pow(10, exponent);
                                            if (negative) number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                                fraction *= 10;
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            bool negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent) exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent) exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent) exponent *= -1;
                            number *= Math.Pow(10, exponent);
                            if (negative) number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative) number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative) number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative) number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal ReadLiteralNumberAsDecimal(char c, ref CharReader reader)
        {
            bool negative = false;
            decimal number;

            switch (c)
            {
                case '0': number = 0; break;
                case '1': number = 1; break;
                case '2': number = 2; break;
                case '3': number = 3; break;
                case '4': number = 4; break;
                case '5': number = 5; break;
                case '6': number = 6; break;
                case '7': number = 7; break;
                case '8': number = 8; break;
                case '9': number = 9; break;
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0': number *= 10; break;
                    case '1': number = number * 10 + 1; break;
                    case '2': number = number * 10 + 2; break;
                    case '3': number = number * 10 + 3; break;
                    case '4': number = number * 10 + 4; break;
                    case '5': number = number * 10 + 5; break;
                    case '6': number = number * 10 + 6; break;
                    case '7': number = number * 10 + 7; break;
                    case '8': number = number * 10 + 8; break;
                    case '9': number = number * 10 + 9; break;
                    case '.':
                        {
                            decimal fraction = 10;
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': break;
                                    case '1': number += 1 / fraction; break;
                                    case '2': number += 2 / fraction; break;
                                    case '3': number += 3 / fraction; break;
                                    case '4': number += 4 / fraction; break;
                                    case '5': number += 5 / fraction; break;
                                    case '6': number += 6 / fraction; break;
                                    case '7': number += 7 / fraction; break;
                                    case '8': number += 8 / fraction; break;
                                    case '9': number += 9 / fraction; break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            bool negativeExponent = false;
                                            decimal exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent) exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative) number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent) exponent *= -1;
                                            number *= (decimal)Math.Pow(10, (double)exponent);
                                            if (negative) number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                                fraction *= 10;
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            bool negativeExponent = false;
                            decimal exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent) exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent) exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative) number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent) exponent *= -1;
                            number *= (decimal)Math.Pow(10, (double)exponent);
                            if (negative) number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative) number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative) number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative) number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsEmpty(char c, ref CharReader reader)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.Read(out c))
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    case '.':
                        {
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.Read(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            switch (c)
                                            {
                                                case '0':
                                                case '1':
                                                case '2':
                                                case '3':
                                                case '4':
                                                case '5':
                                                case '6':
                                                case '7':
                                                case '8':
                                                case '9':
                                                case '+':
                                                case '-':
                                                    break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.Read(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0':
                                                    case '1':
                                                    case '2':
                                                    case '3':
                                                    case '4':
                                                    case '5':
                                                    case '6':
                                                    case '7':
                                                    case '8':
                                                    case '9':
                                                        break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        return;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        return;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            return;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.Read(out c))
                                throw reader.CreateException("Json ended prematurely");

                            switch (c)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '+':
                                case '-':
                                    break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.Read(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            return;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertNullToType(CoreType coreType)
        {
            return coreType switch
            {
                CoreType.String => null,
                CoreType.Boolean => default(bool),
                CoreType.Byte => default(byte),
                CoreType.SByte => default(sbyte),
                CoreType.Int16 => default(short),
                CoreType.UInt16 => default(ushort),
                CoreType.Int32 => default(int),
                CoreType.UInt32 => default(uint),
                CoreType.Int64 => default(long),
                CoreType.UInt64 => default(ulong),
                CoreType.Single => default(float),
                CoreType.Double => default(double),
                CoreType.Decimal => default(decimal),
                CoreType.Char => default(char),
                CoreType.DateTime => default(DateTime),
                CoreType.DateTimeOffset => default(DateTimeOffset),
                CoreType.TimeSpan => default(TimeSpan),
                CoreType.Guid => default(Guid),
                CoreType.BooleanNullable => null,
                CoreType.ByteNullable => null,
                CoreType.SByteNullable => null,
                CoreType.Int16Nullable => null,
                CoreType.UInt16Nullable => null,
                CoreType.Int32Nullable => null,
                CoreType.UInt32Nullable => null,
                CoreType.Int64Nullable => null,
                CoreType.UInt64Nullable => null,
                CoreType.SingleNullable => null,
                CoreType.DoubleNullable => null,
                CoreType.DecimalNullable => null,
                CoreType.CharNullable => null,
                CoreType.DateTimeNullable => null,
                CoreType.DateTimeOffsetNullable => null,
                CoreType.TimeSpanNullable => null,
                CoreType.GuidNullable => null,
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertTrueToType(CoreType coreType)
        {
            switch (coreType)
            {
                case CoreType.String:
                    return "true";

                case CoreType.Boolean:
                case CoreType.BooleanNullable:
                    return true;

                case CoreType.Byte:
                case CoreType.ByteNullable:
                    return (byte)1;
                case CoreType.SByte:
                case CoreType.SByteNullable:
                    return (sbyte)1;
                case CoreType.Int16:
                case CoreType.Int16Nullable:
                    return (short)1;
                case CoreType.UInt16:
                case CoreType.UInt16Nullable:
                    return (ushort)1;
                case CoreType.Int32:
                case CoreType.Int32Nullable:
                    return (int)1;
                case CoreType.UInt32:
                case CoreType.UInt32Nullable:
                    return (uint)1;
                case CoreType.Int64:
                case CoreType.Int64Nullable:
                    return (long)1;
                case CoreType.UInt64:
                case CoreType.UInt64Nullable:
                    return (ulong)1;
                case CoreType.Single:
                case CoreType.SingleNullable:
                    return (float)1;
                case CoreType.Double:
                case CoreType.DoubleNullable:
                    return (double)1;
                case CoreType.Decimal:
                case CoreType.DecimalNullable:
                    return (decimal)1;

                case CoreType.Char: return default(char);
                case CoreType.DateTime: return default(DateTime);
                case CoreType.DateTimeOffset: return default(DateTimeOffset);
                case CoreType.TimeSpan: return default(TimeSpan);
                case CoreType.Guid: return default(Guid);

                case CoreType.CharNullable: return null;
                case CoreType.DateTimeNullable: return null;
                case CoreType.DateTimeOffsetNullable: return null;
                case CoreType.TimeSpanNullable: return null;
                case CoreType.GuidNullable: return null;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertFalseToType(CoreType coreType)
        {
            switch (coreType)
            {
                case CoreType.String:
                    return "false";

                case CoreType.Boolean:
                case CoreType.BooleanNullable:
                    return false;

                case CoreType.Byte:
                case CoreType.ByteNullable:
                    return (byte)0;
                case CoreType.SByte:
                case CoreType.SByteNullable:
                    return (sbyte)0;
                case CoreType.Int16:
                case CoreType.Int16Nullable:
                    return (short)0;
                case CoreType.UInt16:
                case CoreType.UInt16Nullable:
                    return (ushort)0;
                case CoreType.Int32:
                case CoreType.Int32Nullable:
                    return (int)0;
                case CoreType.UInt32:
                case CoreType.UInt32Nullable:
                    return (uint)0;
                case CoreType.Int64:
                case CoreType.Int64Nullable:
                    return (long)0;
                case CoreType.UInt64:
                case CoreType.UInt64Nullable:
                    return (ulong)0;
                case CoreType.Single:
                case CoreType.SingleNullable:
                    return (float)0;
                case CoreType.Double:
                case CoreType.DoubleNullable:
                    return (double)0;
                case CoreType.Decimal:
                case CoreType.DecimalNullable:
                    return (decimal)0;

                case CoreType.Char: return default(char);
                case CoreType.DateTime: return default(DateTime);
                case CoreType.DateTimeOffset: return default(DateTimeOffset);
                case CoreType.TimeSpan: return default(TimeSpan);
                case CoreType.Guid: return default(Guid);

                case CoreType.CharNullable: return null;
                case CoreType.DateTimeNullable: return null;
                case CoreType.DateTimeOffsetNullable: return null;
                case CoreType.TimeSpanNullable: return null;
                case CoreType.GuidNullable: return null;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}