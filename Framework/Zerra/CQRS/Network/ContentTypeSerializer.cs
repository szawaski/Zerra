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
    /// <summary>
    /// A helper class to serialize data using the specified ContentType to determine the data format.
    /// </summary>
    public static class ContentTypeSerializer
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            IndexType = ByteSerializerIndexType.MemberNames,
            IgnoreIndexAttribute = true
        };

        private static readonly JsonSerializerOptions jsonSerializerNamelessOptions = new()
        {
            Nameless = true
        };

        /// <summary>
        /// Serializes data selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="obj">The object to be serialized.</param>
        /// <returns>The serialized data in bytes.</returns>
        public static byte[] Serialize(ContentType contentType, object? obj)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Serialize(obj, byteSerializerOptions),
                ContentType.Json => JsonSerializer.SerializeBytes(obj),
                ContentType.JsonNameless => JsonSerializer.SerializeBytes(obj, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// Deserializes data selecting a serializer from the specified ContentType
        /// </summary>
        /// <typeparam name="T">The type to which the data will deserialize.</typeparam>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="bytes">The serialized data in bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static T? Deserialize<T>(ContentType contentType, byte[] bytes)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions),
                ContentType.Json => JsonSerializer.Deserialize<T>(bytes),
                ContentType.JsonNameless => JsonSerializer.Deserialize<T>(bytes, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// Deserializes data selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="type">The type to which the data will deserialize.</param>
        /// <param name="bytes">The serialized data in bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static object? Deserialize(ContentType contentType, Type type, byte[] bytes)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize(type, bytes, byteSerializerOptions),
                ContentType.Json => JsonSerializer.Deserialize(type, bytes),
                ContentType.JsonNameless => JsonSerializer.Deserialize(type, bytes, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Serializes data selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="obj">The object to be serialized.</param>
        public static void Serialize(ContentType contentType, Stream stream, object? obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    ByteSerializer.Serialize(stream, obj, byteSerializerOptions);
                    return;
                case ContentType.Json:
                    JsonSerializer.Serialize(stream, obj);
                    return;
                case ContentType.JsonNameless:
                    JsonSerializer.Serialize(stream, obj, jsonSerializerNamelessOptions);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type to which the data will deserialize.</typeparam>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static T? Deserialize<T>(ContentType contentType, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<T>(stream, byteSerializerOptions),
                ContentType.Json => JsonSerializer.Deserialize<T>(stream),
                ContentType.JsonNameless => JsonSerializer.Deserialize<T>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="type">The type to which the data will deserialize.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static object? Deserialize(ContentType contentType, Type type, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize(type, stream, byteSerializerOptions),
                ContentType.Json => JsonSerializer.Deserialize(type, stream),
                ContentType.JsonNameless => JsonSerializer.Deserialize(type, stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <param name="obj">The object to be serialized.</param>
        /// <returns></returns>
        public static Task SerializeAsync(ContentType contentType, Stream stream, object? obj)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.SerializeAsync(stream, obj, byteSerializerOptions),
                ContentType.Json => JsonSerializer.SerializeAsync(stream, obj),
                ContentType.JsonNameless => JsonSerializer.SerializeAsync(stream, obj, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The type to which the data will deserialize.</typeparam>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static Task<T?> DeserializeAsync<T>(ContentType contentType, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.DeserializeAsync<T>(stream, byteSerializerOptions),
                ContentType.Json => JsonSerializer.DeserializeAsync<T>(stream),
                ContentType.JsonNameless => JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="type">The type to which the data will deserialize.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized data object.</returns>
        public static Task<object?> DeserializeAsync(ContentType contentType, Type type, Stream stream)
        {
            return contentType switch
            {
                ContentType.Bytes => ByteSerializer.DeserializeAsync(type, stream, byteSerializerOptions),
                ContentType.Json => JsonSerializer.DeserializeAsync(type, stream),
                ContentType.JsonNameless => JsonSerializer.DeserializeAsync(type, stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Serializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
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
                    content.ErrorString = JsonSerializer.Serialize(ex, errorType);
                    JsonSerializer.Serialize(stream, content);
                    return;
                case ContentType.JsonNameless:
                    content.ErrorString = JsonSerializer.Serialize(ex, errorType, jsonSerializerNamelessOptions);
                    JsonSerializer.Serialize(stream, content, jsonSerializerNamelessOptions);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Deserializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized Exception.</returns>
        public static Exception DeserializeException(ContentType contentType, Stream stream)
        {
            ExceptionContent? content = contentType switch
            {
                ContentType.Bytes => ByteSerializer.Deserialize<ExceptionContent>(stream),
                ContentType.Json => JsonSerializer.Deserialize<ExceptionContent>(stream),
                ContentType.JsonNameless => JsonSerializer.Deserialize<ExceptionContent>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
            if (content is null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType is not null)
            {
                try
                {
                    var type = Discovery.GetTypeFromName(content.ErrorType);
                    switch (contentType)
                    {
                        case ContentType.Bytes:
                            if (content.ErrorBytes is not null && content.ErrorBytes.Length > 0)
                                ex = (Exception?)ByteSerializer.Deserialize(type, content.ErrorBytes, byteSerializerOptions);
                            break;
                        case ContentType.Json:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializer.Deserialize(type, content.ErrorString);
                            break;
                        case ContentType.JsonNameless:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializer.Deserialize(type, content.ErrorString, jsonSerializerNamelessOptions);
                            break;
                    }
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }

        /// <summary>
        /// Serializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        /// <returns>The deserialized Exception.</returns>
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
                    content.ErrorString = JsonSerializer.Serialize(ex, errorType);
                    return JsonSerializer.SerializeAsync(stream, content);
                case ContentType.JsonNameless:
                    content.ErrorString = JsonSerializer.Serialize(ex, errorType, jsonSerializerNamelessOptions);
                    return JsonSerializer.SerializeAsync(stream, content, jsonSerializerNamelessOptions);
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Deserializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized Exception.</returns>
        public static async Task<Exception> DeserializeExceptionAsync(ContentType contentType, Stream stream)
        {
            ExceptionContent? content = contentType switch
            {
                ContentType.Bytes => await ByteSerializer.DeserializeAsync<ExceptionContent>(stream),
                ContentType.Json => await JsonSerializer.DeserializeAsync<ExceptionContent>(stream),
                ContentType.JsonNameless => await JsonSerializer.DeserializeAsync<ExceptionContent>(stream, jsonSerializerNamelessOptions),
                _ => throw new NotImplementedException(),
            };
            if (content is null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType is not null)
            {
                try
                {
                    var type = Discovery.GetTypeFromName(content.ErrorType);
                    switch (contentType)
                    {
                        case ContentType.Bytes:
                            if (content.ErrorBytes is not null && content.ErrorBytes.Length > 0)
                                ex = (Exception?)ByteSerializer.Deserialize(type, content.ErrorBytes, byteSerializerOptions);
                            break;
                        case ContentType.Json:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializer.Deserialize(type, content.ErrorString);
                            break;
                        case ContentType.JsonNameless:
                            if (!String.IsNullOrEmpty(content.ErrorString))
                                ex = (Exception?)JsonSerializer.Deserialize(type, content.ErrorString, jsonSerializerNamelessOptions);
                            break;
                    }
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }
    }
}