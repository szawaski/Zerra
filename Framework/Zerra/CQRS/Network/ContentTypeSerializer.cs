// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    public static class ContentTypeSerializer
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            UsePropertyNames = true,
            IgnoreIndexAttribute = true
        };

        private static readonly JsonSerializerOptionsOld jsonSerializerNamelessOptions = new()
        {
            Nameless = true
        };

        public static byte[] Serialize(ContentType contentType, object? obj)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Serialize(obj, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.SerializeBytes(obj),
                ContentType.JsonNameless => JsonSerializerOld.SerializeBytes(obj, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        public static T? Deserialize<T>(ContentType contentType, byte[] bytes)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.Deserialize<T>(bytes),
                ContentType.JsonNameless => JsonSerializerOld.Deserialize<T>(bytes, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        public static object? Deserialize<T>(ContentType contentType, Type type, byte[] bytes)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize(type, bytes, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.Deserialize(type, bytes),
                ContentType.JsonNameless => JsonSerializerOld.Deserialize(type, bytes, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        public static void Serialize(ContentType contentType, Stream stream, object? obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    ByteSerializer.Serialize(stream, obj, byteSerializerOptions);
                    return;
                case ContentType.Json:
                    JsonSerializerOld.Serialize(stream, obj);
                    return;
                case ContentType.JsonNameless:
                    JsonSerializerOld.Serialize(stream, obj, jsonSerializerNamelessOptions);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        public static T? Deserialize<T>(ContentType contentType, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<T>(stream, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.Deserialize<T>(stream),
                ContentType.JsonNameless => JsonSerializerOld.Deserialize<T>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        public static object? Deserialize(ContentType contentType, Type type, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize(type, stream, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.Deserialize(type, stream),
                ContentType.JsonNameless => JsonSerializerOld.Deserialize(type, stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        public static Task SerializeAsync(ContentType contentType, Stream stream, object? obj)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.SerializeAsync(stream, obj, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.SerializeAsync(stream, obj),
                ContentType.JsonNameless => JsonSerializerOld.SerializeAsync(stream, obj, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        public static Task<T?> DeserializeAsync<T>(ContentType contentType, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.DeserializeAsync<T>(stream, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.DeserializeAsync<T>(stream),
                ContentType.JsonNameless => JsonSerializerOld.DeserializeAsync<T>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        public static Task<object?> DeserializeAsync(ContentType contentType, Type type, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.DeserializeAsync(type, stream, byteSerializerOptions),
                ContentType.Json => JsonSerializerOld.DeserializeAsync(type, stream),
                ContentType.JsonNameless => JsonSerializerOld.DeserializeAsync(type, stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        public static void SerializeException(ContentType contentType, Stream stream, Exception ex)
        {
            var errorType = ex.GetType();
            var content = new ExceptionContent()
            {
                ErrorMessage = ex.Message,
                ErrorType = errorType.FullName
            };

            switch (contentType)
            {
                case ContentType.Bytes:
                    content.ErrorBytes = ByteSerializer.Serialize(ex, errorType, byteSerializerOptions);
                    ByteSerializer.Serialize(stream, content);
                    return;
                case ContentType.Json:
                    content.ErrorString = JsonSerializerOld.Serialize(ex, errorType);
                    JsonSerializerOld.Serialize(stream, content);
                    return;
                case ContentType.JsonNameless:
                    content.ErrorString = JsonSerializerOld.Serialize(ex, errorType, jsonSerializerNamelessOptions);
                    JsonSerializerOld.Serialize(stream, content, jsonSerializerNamelessOptions);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        public static Exception DeserializeException(ContentType contentType, Stream stream)
        {
            ExceptionContent? content = contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<ExceptionContent>(stream),
                ContentType.Json => JsonSerializerOld.Deserialize<ExceptionContent>(stream),
                ContentType.JsonNameless => JsonSerializerOld.Deserialize<ExceptionContent>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
            if (content == null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType != null)
            {
                try
                {
                    var type = Discovery.GetTypeFromName(content.ErrorType);
                    switch (contentType)
                    {
                        case ContentType.Bytes:
                            if (content.ErrorBytes != null && content.ErrorBytes.Length > 0)
                                ex = (Exception?)ByteSerializer.Deserialize(type, content.ErrorBytes, byteSerializerOptions);
                            break;
                        case ContentType.Json:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializerOld.Deserialize(type, content.ErrorString);
                            break;
                        case ContentType.JsonNameless:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializerOld.Deserialize(type, content.ErrorString, jsonSerializerNamelessOptions);
                            break;
                    }
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }

        public static Task SerializeExceptionAsync(ContentType contentType, Stream stream, Exception ex)
        {
            var errorType = ex.GetType();
            var content = new ExceptionContent()
            {
                ErrorMessage = ex.Message,
                ErrorType = errorType.FullName
            };

            switch (contentType)
            {
                case ContentType.Bytes:
                    content.ErrorBytes = ByteSerializer.Serialize(ex, errorType, byteSerializerOptions);
                    return ByteSerializer.SerializeAsync(stream, content);
                case ContentType.Json:
                    content.ErrorString = JsonSerializerOld.Serialize(ex, errorType);
                    return JsonSerializerOld.SerializeAsync(stream, content);
                case ContentType.JsonNameless:
                    content.ErrorString = JsonSerializerOld.Serialize(ex, errorType, jsonSerializerNamelessOptions);
                    return JsonSerializerOld.SerializeAsync(stream, content, jsonSerializerNamelessOptions);
                default:
                    throw new NotImplementedException();
            }
        }
        public static async Task<Exception> DeserializeExceptionAsync(ContentType contentType, Stream stream)
        {
            ExceptionContent? content = contentType switch
            {
                ContentType.Bytes => await ByteSerializer.DeserializeAsync<ExceptionContent>(stream),
                ContentType.Json => await JsonSerializerOld.DeserializeAsync<ExceptionContent>(stream),
                ContentType.JsonNameless => await JsonSerializerOld.DeserializeAsync<ExceptionContent>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
            if (content == null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType != null)
            {
                try
                {
                    var type = Discovery.GetTypeFromName(content.ErrorType);
                    switch (contentType)
                    {
                        case ContentType.Bytes:
                            if (content.ErrorBytes != null && content.ErrorBytes.Length > 0)
                                ex = (Exception?)ByteSerializer.Deserialize(type, content.ErrorBytes, byteSerializerOptions);
                            break;
                        case ContentType.Json:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializerOld.Deserialize(type, content.ErrorString);
                            break;
                        case ContentType.JsonNameless:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializerOld.Deserialize(type, content.ErrorString, jsonSerializerNamelessOptions);
                            break;
                    }
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }
    }
}