// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null, Graph? graph = null) { return Deserialize<T>(json.AsSpan(), options, graph); }
        public static object? Deserialize(Type type, string json, JsonSerializerOptions? options = null, Graph? graph = null) { return Deserialize(type, json.AsSpan(), options, graph); }

        public static T? Deserialize<T>(ReadOnlySpan<char> json, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            var type = typeof(T);
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (json == null || json.Length == 0)
                return (T?)ConvertStringToType(String.Empty, typeDetails);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriter();
            try
            {
                if (!reader.TryReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromStringJson(c, ref reader, ref decodeBuffer, typeDetails, graph, ref optionsStruct);
                if (reader.HasMoreChars())
                {
                    if (reader.TryReadSkipWhiteSpace(out _))
                        throw reader.CreateException("Unexpected character and end of json");
                }
                return (T?)value;
            }
            finally
            {
                decodeBuffer.Dispose();
            }
        }
        public static object? Deserialize(Type type, ReadOnlySpan<char> json, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (json == null || json.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

            var reader = new CharReader(json);
            var decodeBuffer = new CharWriter();
            try
            {
                if (!reader.TryReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromStringJson(c, ref reader, ref decodeBuffer, typeDetails, graph, ref optionsStruct);
                if (reader.HasMoreChars())
                {
                    if (reader.TryReadSkipWhiteSpace(out _))
                        throw reader.CreateException("Unexpected character and end of json");
                }
                return value;
            }
            finally
            {

                decodeBuffer.Dispose();
            }
        }

        public static T? Deserialize<T>(byte[] bytes, JsonSerializerOptions? options = null, Graph? graph = null) { return Deserialize<T>(bytes.AsSpan(), options, graph); }
        public static object? Deserialize(Type type, byte[] bytes, JsonSerializerOptions? options = null, Graph? graph = null) { return Deserialize(type, bytes.AsSpan(), options, graph); }

        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            var obj = Deserialize(typeof(T), bytes, options, graph);
            if (obj == null)
                return default;
            return (T)obj;
        }
        public static object? Deserialize(Type type, ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (bytes == null || bytes.Length == 0)
                return ConvertStringToType(String.Empty, typeDetails);

#if NETSTANDARD2_0
            var chars = Encoding.UTF8.GetChars(bytes.ToArray(), 0, bytes.Length).AsSpan();
#else
            Span<char> chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
            var count = Encoding.UTF8.GetChars(bytes, chars);
            chars = chars.Slice(0, count);
#endif

            var reader = new CharReader(chars);
            var decodeBuffer = new CharWriter();
            try
            {
                if (!reader.TryReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromStringJson(c, ref reader, ref decodeBuffer, typeDetails, graph, ref optionsStruct);
                if (reader.HasMoreChars())
                {
                    if (reader.TryReadSkipWhiteSpace(out _))
                        throw reader.CreateException("Unexpected character and end of json");
                }
                return value;
            }
            finally
            {
                decodeBuffer.Dispose();
            }
        }

        public static JsonObject DeserializeJsonObject(string json, Graph? graph = null)
        {
            if (json.Length == 0)
                return new JsonObject(null, true);

            var decodeBuffer = new CharWriter();
            try
            {
                var reader = new CharReader(json);
                if (!reader.TryReadSkipWhiteSpace(out var c))
                    throw reader.CreateException("Json ended prematurely");

                var value = FromStringJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
                if (reader.HasMoreChars())
                {
                    if (reader.TryReadSkipWhiteSpace(out _))
                        throw reader.CreateException("Unexpected character and end of json");
                }
                return value;
            }
            finally
            {
                decodeBuffer.Dispose();
            }
        }

        private static object? FromStringJson(char c, ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail? typeDetail, Graph? graph, ref OptionsStruct options)
        {
            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = TypeAnalyzer.GetTypeDetail(emptyImplementationType);
            }

            switch (c)
            {
                case '"':
                    var value = FromStringString(ref reader, ref decodeBuffer);
                    return ConvertStringToType(value, typeDetail);
                case '{':
                    if (typeDetail != null && typeDetail.SpecialType == SpecialType.Dictionary)
                        return FromStringJsonDictionary(ref reader, ref decodeBuffer, typeDetail, graph, ref options);
                    else
                        return FromStringJsonObject(ref reader, ref decodeBuffer, typeDetail, graph, ref options);
                case '[':
                    if (!options.Nameless || (typeDetail != null && typeDetail.IsIEnumerableGeneric))
                        return FromStringJsonArray(ref reader, ref decodeBuffer, typeDetail, graph, ref options);
                    else
                        return FromStringJsonArrayNameless(ref reader, ref decodeBuffer, typeDetail, graph, ref options);
                default:
                    return FromStringLiteral(c, ref reader, ref decodeBuffer, typeDetail);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromStringJsonObject(ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail? typeDetail, Graph? graph, ref OptionsStruct options)
        {
            var obj = typeDetail != null && typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var propertyName = FromStringString(ref reader, ref decodeBuffer);
                        if (String.IsNullOrWhiteSpace(propertyName))
                            throw reader.CreateException("Unexpected character");

                        FromStringPropertySeperator(ref reader);

                        if (!reader.TryReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        if (obj != null)
                        {
                            if (TryGetMember(typeDetail!, propertyName, out var memberDetail))
                            {
                                var propertyGraph = graph?.GetChildGraph(memberDetail.Name);
                                var value = FromStringJson(c, ref reader, ref decodeBuffer, memberDetail.TypeDetail, propertyGraph, ref options);
                                if (value != null && memberDetail.HasSetterBoxed)
                                {
                                    //special case nullable enum
                                    if (memberDetail.TypeDetail.IsNullable && memberDetail.TypeDetail.InnerTypeDetails[0].EnumUnderlyingType.HasValue)
                                        value = Enum.ToObject(memberDetail.TypeDetail.InnerTypeDetails[0].Type, value);

                                    if (graph != null)
                                    {
                                        if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                        {
                                            if (graph.HasLocalProperty(memberDetail.Name))
                                                memberDetail.SetterBoxed(obj, value);
                                        }
                                        else
                                        {
                                            if (propertyGraph != null)
                                                memberDetail.SetterBoxed(obj, value);
                                        }
                                    }
                                    else
                                    {
                                        memberDetail.SetterBoxed(obj, value);
                                    }
                                }
                            }
                            else
                            {
                                _ = FromStringJson(c, ref reader, ref decodeBuffer, null, null, ref options);
                            }
                        }
                        else
                        {
                            _ = FromStringJson(c, ref reader, ref decodeBuffer, null, null, ref options);
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
        private static object? FromStringJsonDictionary(ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail typeDetail, Graph? graph, ref OptionsStruct options)
        {
            object? obj = null;
            MethodDetail? method = null;
            object?[]? addMethodArgs = null;
            if (typeDetail != null)
            {
                if (typeDetail.Type.IsInterface)
                {
                    typeDetail = TypeAnalyzer.GetGenericTypeDetail(dictionaryType, (Type[])typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                }

                obj = typeDetail.CreatorBoxed();
                method = typeDetail.GetMethod("Add");
                addMethodArgs = new object?[2];
            }

            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var dictionaryKey = FromStringJson(c, ref reader, ref decodeBuffer, typeDetail?.InnerTypeDetails[0], null, ref options);

                        FromStringPropertySeperator(ref reader);

                        if (!reader.TryReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        if (obj != null)
                        {
                            //Dictionary Special Case
                            var value = FromStringJson(c, ref reader, ref decodeBuffer, typeDetail!.InnerTypeDetails[1], null, ref options);
                            if (typeDetail.InnerTypeDetails[0].CoreType.HasValue)
                            {
                                addMethodArgs![0] = dictionaryKey;
                                addMethodArgs[1] = value;
                                _ = method!.Caller(obj, addMethodArgs);
                            }
                        }
                        else
                        {
                            _ = FromStringJson(c, ref reader, ref decodeBuffer, null, null, ref options);
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
        private static object? FromStringJsonArray(ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail? typeDetail, Graph? graph, ref OptionsStruct options)
        {
            object? collection = null;
            MethodDetail? addMethod = null;
            object?[]? addMethodArgs = null;
            TypeDetail? arrayElementType = null;
            if (typeDetail != null && typeDetail.IsIEnumerableGeneric)
            {
                arrayElementType = typeDetail.IEnumerableGenericInnerTypeDetail;
                if (typeDetail.Type.IsArray)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, arrayElementType.Type));
                    collection = genericListType.CreatorBoxed();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsIList && typeDetail.Type.IsInterface)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, arrayElementType.Type));
                    collection = genericListType.CreatorBoxed();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsIList && !typeDetail.Type.IsInterface)
                {
                    collection = typeDetail.CreatorBoxed();
                    addMethod = typeDetail.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsISet && typeDetail.Type.IsInterface)
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericHashSetType, arrayElementType.Type));
                    collection = genericListType.CreatorBoxed();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else if (typeDetail.IsISet && !typeDetail.Type.IsInterface)
                {
                    collection = typeDetail.CreatorBoxed();
                    addMethod = typeDetail.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
                else
                {
                    var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, arrayElementType.Type));
                    collection = genericListType.CreatorBoxed();
                    addMethod = genericListType.GetMethod("Add");
                    addMethodArgs = new object[1];
                }
            }

            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        if (collection == null)
                            return null;
                        if (typeDetail != null && typeDetail.Type.IsArray && arrayElementType != null)
                        {
                            var list = (IList)collection;
                            var array = Array.CreateInstance(arrayElementType.Type, list.Count);
                            for (var i = 0; i < list.Count; i++)
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
                        var value = FromStringJson(c, ref reader, ref decodeBuffer, arrayElementType, graph, ref options);
                        if (collection != null)
                        {
                            //special case nullable enum
                            if (arrayElementType!.IsNullable && arrayElementType.InnerTypeDetails[0].EnumUnderlyingType.HasValue && value != null)
                                value = Enum.ToObject(arrayElementType.InnerTypeDetails[0].Type, value);

                            addMethodArgs![0] = value;
                            _ = addMethod!.Caller(collection, addMethodArgs);
                        }
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromStringJsonArrayNameless(ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail? typeDetail, Graph? graph, ref OptionsStruct options)
        {
            var obj = typeDetail != null && typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
            var canExpectComma = false;
            var propertyIndexForNameless = 0;
            while (reader.TryReadSkipWhiteSpace(out var c))
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
                            if (memberDetail != null && memberDetail.HasSetterBoxed)
                            {
                                var propertyGraph = graph?.GetChildGraph(memberDetail.Name);
                                var value = FromStringJson(c, ref reader, ref decodeBuffer, memberDetail?.TypeDetail, propertyGraph, ref options);
                                if (memberDetail!.TypeDetail.SpecialType.HasValue && memberDetail.TypeDetail.SpecialType == SpecialType.Dictionary)
                                {
                                    var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, memberDetail.TypeDetail.IEnumerableGenericInnerType);
                                    object dictionary;
                                    if (memberDetail.TypeDetail.Type.IsInterface)
                                    {
                                        var dictionaryGenericType = TypeAnalyzer.GetGenericType(dictionaryType, (Type[])memberDetail.TypeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                                        dictionary = Instantiator.Create(dictionaryGenericType, new Type[] { innerItemEnumerable }, value);
                                    }
                                    else
                                    {
                                        dictionary = Instantiator.Create(typeDetail!.Type, new Type[] { innerItemEnumerable }, value);
                                    }
                                    memberDetail.SetterBoxed(obj, dictionary);
                                }
                                else
                                {
                                    if (value != null)
                                    {
                                        //special case nullable enum
                                        if (memberDetail.TypeDetail.IsNullable && memberDetail.TypeDetail.InnerTypeDetails[0].EnumUnderlyingType.HasValue)
                                            value = Enum.ToObject(memberDetail.TypeDetail.InnerTypeDetails[0].Type, value);

                                        if (graph != null)
                                        {
                                            if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                            {
                                                if (graph.HasLocalProperty(memberDetail.Name))
                                                    memberDetail.SetterBoxed(obj, value);
                                            }
                                            else
                                            {
                                                if (propertyGraph != null)
                                                    memberDetail.SetterBoxed(obj, value);
                                            }
                                        }
                                        else
                                        {
                                            memberDetail.SetterBoxed(obj, value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _ = FromStringJson(c, ref reader, ref decodeBuffer, null, null, ref options);
                            }
                        }
                        else
                        {
                            _ = FromStringJson(c, ref reader, ref decodeBuffer, null, null, ref options);
                        }
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromStringLiteral(char c, ref CharReader reader, ref CharWriter decodeBuffer, TypeDetail? typeDetail)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertNullToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 't':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'r')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertTrueToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 'f':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'a')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 's')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertFalseToType(typeDetail.CoreType.Value);
                        return null;
                    }
                default:
                    {
                        if (typeDetail != null)
                        {
                            if (typeDetail.CoreType == CoreType.String)
                            {
                                var value = FromStringLiteralNumberAsString(c, ref reader, ref decodeBuffer);
                                return value;
                            }
                            else if (typeDetail.CoreType.HasValue)
                            {
                                return FromStringLiteralNumberAsType(c, typeDetail.CoreType.Value, ref reader, ref decodeBuffer);
                            }
                            else if (typeDetail.EnumUnderlyingType.HasValue)
                            {
                                return FromStringLiteralNumberAsType(c, typeDetail.EnumUnderlyingType.Value, ref reader, ref decodeBuffer);
                            }
                        }
                        FromStringLiteralNumberAsEmpty(c, ref reader);
                        return null;
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }

        private static JsonObject FromStringJsonToJsonObject(char c, ref CharReader reader, ref CharWriter decodeBuffer, Graph? graph)
        {
            switch (c)
            {
                case '"':
                    var value = FromStringString(ref reader, ref decodeBuffer);
                    return new JsonObject(value, false);
                case '{':
                    return FromStringObjectToJsonObject(ref reader, ref decodeBuffer, graph);
                case '[':
                    return FromStringArrayToJsonObject(ref reader, ref decodeBuffer, graph);
                default:
                    return FromStringLiteralToJsonObject(c, ref reader, ref decodeBuffer);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject FromStringObjectToJsonObject(ref CharReader reader, ref CharWriter decodeBuffer, Graph? graph)
        {
            var properties = new Dictionary<string, JsonObject>();
            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var propertyName = FromStringString(ref reader, ref decodeBuffer);
                        if (String.IsNullOrWhiteSpace(propertyName))
                            throw reader.CreateException("Unexpected character");

                        FromStringPropertySeperator(ref reader);

                        if (!reader.TryReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        var propertyGraph = graph?.GetChildGraph(propertyName);
                        var value = FromStringJsonToJsonObject(c, ref reader, ref decodeBuffer, propertyGraph);

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
                                    if (graph.HasChild(propertyName))
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
        private static JsonObject FromStringArrayToJsonObject(ref CharReader reader, ref CharWriter decodeBuffer, Graph? graph)
        {
            var arrayList = new List<JsonObject>();

            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
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
                        var value = FromStringJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
                        //if (arrayList != null)
                        arrayList.Add(value);
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject FromStringLiteralToJsonObject(char c, ref CharReader reader, ref CharWriter decodeBuffer)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject(null, true);
                    }
                case 't':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'r')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("true", true);
                    }
                case 'f':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'a')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 's')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("false", true);
                    }
                default:
                    {
                        var value = FromStringLiteralNumberAsString(c, ref reader, ref decodeBuffer);
                        return new JsonObject(value, true);
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FromStringPropertySeperator(ref CharReader reader)
        {
            if (!reader.TryReadSkipWhiteSpace(out var c))
                throw reader.CreateException("Json ended prematurely");
            if (c == ':')
                return;
            throw reader.CreateException("Unexpected character");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? FromStringString(ref CharReader reader, ref CharWriter decodeBuffer)
        {
            char c;
            while (reader.TryReadSpanUntil(out var s, '\"', '\\'))
            {
                decodeBuffer.Write(s.Slice(0, s.Length - 1));
                c = s[s.Length - 1];
                switch (c)
                {
                    case '\"':
                        {
                            var result = decodeBuffer.ToString();
                            decodeBuffer.Clear();
                            return result;
                        }
                    case '\\':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

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
                                    if (!reader.TryReadSpan(out var unicodeSpan, 4))
                                        throw reader.CreateException("Json ended prematurely");
                                    if (!lowUnicodeHexToChar.TryGetValue(unicodeSpan.ToString(), out var unicodeChar))
                                        throw reader.CreateException("Incomplete escape sequence");
                                    decodeBuffer.Write(unicodeChar);
                                    break;
                                default:
                                    decodeBuffer.Write(c);
                                    break;
                            }
                        }
                        break;
                }
            }

            throw reader.CreateException("Json ended prematurely");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? FromStringLiteralNumberAsType(char c, CoreType coreType, ref CharReader reader, ref CharWriter decodeBuffer)
        {
            unchecked
            {
                switch (coreType)
                {
                    case CoreType.String:
                        return FromStringLiteralNumberAsString(c, ref reader, ref decodeBuffer);
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        return (byte)FromStringLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        return (sbyte)FromStringLiteralNumberAsInt64(c, ref reader);
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        return (short)FromStringLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        return (ushort)FromStringLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        return (int)FromStringLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        return (uint)FromStringLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        return (long)FromStringLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        return (ulong)FromStringLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        return (float)FromStringLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        return FromStringLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        return FromStringLiteralNumberAsDecimal(c, ref reader);
                }
            }
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FromStringLiteralNumberAsString(char c, ref CharReader reader, ref CharWriter decodeBuffer)
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
                    //nothing
                    break;
                default: throw reader.CreateException("Unexpected character");
            }
            decodeBuffer.Write(c);

            while (reader.TryRead(out c))
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
                        decodeBuffer.Write(c);
                        break;
                    case '.':
                        {
                            decodeBuffer.Write('.');

                            while (reader.TryRead(out c))
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
                                        decodeBuffer.Write(c);
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            decodeBuffer.Write('E');

                                            if (!reader.TryRead(out c))
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
                                                    decodeBuffer.Write(c);
                                                    break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
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
                                                        decodeBuffer.Write(c);
                                                        break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        {
                                                            var result = decodeBuffer.ToString();
                                                            decodeBuffer.Clear();
                                                            return result;
                                                        }
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        {
                                                            var result = decodeBuffer.ToString();
                                                            decodeBuffer.Clear();
                                                            return result;
                                                        }
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            {
                                                var result = decodeBuffer.ToString();
                                                decodeBuffer.Clear();
                                                return result;
                                            }
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        {
                                            var result = decodeBuffer.ToString();
                                            decodeBuffer.Clear();
                                            return result;
                                        }
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        {
                                            var result = decodeBuffer.ToString();
                                            decodeBuffer.Clear();
                                            return result;
                                        }
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            decodeBuffer.Write('E');

                            if (!reader.TryRead(out c))
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
                                    decodeBuffer.Write(c);
                                    break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
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
                                        {
                                            var result = decodeBuffer.ToString();
                                            decodeBuffer.Clear();
                                            return result;
                                        }
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        {
                                            var result = decodeBuffer.ToString();
                                            decodeBuffer.Clear();
                                            return result;
                                        }
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            {
                                var result = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                return result;
                            }
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        {
                            var result = decodeBuffer.ToString();
                            decodeBuffer.Clear();
                            return result;
                        }
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        {
                            var result = decodeBuffer.ToString();
                            decodeBuffer.Clear();
                            return result;
                        }
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            {
                var result = decodeBuffer.ToString();
                decodeBuffer.Clear();
                return result;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long FromStringLiteralNumberAsInt64(char c, ref CharReader reader)
        {
            var negative = false;
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

            while (reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
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
                                            while (reader.TryRead(out c))
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
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= (long)Math.Pow(10, (double)exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
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
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
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
                            while (reader.TryRead(out c))
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
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= (long)Math.Pow(10, (double)exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FromStringLiteralNumberAsUInt64(char c, ref CharReader reader)
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
            while (reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
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
                                            while (reader.TryRead(out c))
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
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
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
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
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
                            while (reader.TryRead(out c))
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
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
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
        private static double FromStringLiteralNumberAsDouble(char c, ref CharReader reader)
        {
            var negative = false;
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

            while (reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
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
                                            while (reader.TryRead(out c))
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
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= Math.Pow(10, exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
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
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
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
                            while (reader.TryRead(out c))
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
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= Math.Pow(10, exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal FromStringLiteralNumberAsDecimal(char c, ref CharReader reader)
        {
            var negative = false;
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

            while (reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
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
                                            while (reader.TryRead(out c))
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
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= (decimal)Math.Pow(10, (double)exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
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
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
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
                            while (reader.TryRead(out c))
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
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= (decimal)Math.Pow(10, (double)exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FromStringLiteralNumberAsEmpty(char c, ref CharReader reader)
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

            while (reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
                                            if (!reader.TryRead(out c))
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
                                            while (reader.TryRead(out c))
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
                            if (!reader.TryRead(out c))
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
                            while (reader.TryRead(out c))
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
    }
}