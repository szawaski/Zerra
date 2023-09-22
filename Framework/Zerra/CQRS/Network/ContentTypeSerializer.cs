﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public static class ContentTypeSerializer
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new ByteSerializerOptions()
        {
            UsePropertyNames = true,
            IgnoreIndexAttribute = true
        };

        public static byte[] Serialize(ContentType contentType, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.Serialize(obj, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.SerializeBytes(obj);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.SerializeNamelessBytes(obj);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static T Deserialize<T>(ContentType contentType, byte[] bytes)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.Deserialize<T>(bytes);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.DeserializeNameless<T>(bytes);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static object Deserialize<T>(ContentType contentType, Type type, byte[] bytes)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.Deserialize(type, bytes, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.Deserialize(type, bytes);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.DeserializeNameless(type, bytes);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static void Serialize(ContentType contentType, Stream stream, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        ByteSerializer.Serialize(stream, obj, byteSerializerOptions);
                        return;
                    }
                case ContentType.Json:
                    {
                        JsonSerializer.Serialize(stream, obj);
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        JsonSerializer.SerializeNameless(stream, obj);
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static T Deserialize<T>(ContentType contentType, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.Deserialize<T>(stream, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.Deserialize<T>(stream);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.DeserializeNameless<T>(stream);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static object Deserialize(ContentType contentType, Type type, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.Deserialize(type, stream, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.Deserialize(type, stream);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.DeserializeNameless(type, stream);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static Task SerializeAsync(ContentType contentType, Stream stream, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.SerializeAsync(stream, obj, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.SerializeAsync(stream, obj);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.SerializeNamelessAsync(stream, obj);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static Task<T> DeserializeAsync<T>(ContentType contentType, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.DeserializeAsync<T>(stream, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.DeserializeAsync<T>(stream);
                    }
                case ContentType.JsonNameless:
                    {

                        return JsonSerializer.DeserializeNamelessAsync<T>(stream);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static Task<object> DeserializeAsync(ContentType contentType, Type type, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.DeserializeAsync(type, stream, byteSerializerOptions);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.DeserializeAsync(type, stream);
                    }
                case ContentType.JsonNameless:
                    {

                        return JsonSerializer.DeserializeNamelessAsync(type, stream);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static void SerializeException(ContentType contentType, Stream stream, Exception ex)
        {
            var model = new ExceptionModel()
            {
                Message = ex.Message
            };

            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        ByteSerializer.Serialize(stream, model);
                        return;
                    }
                case ContentType.Json:
                    {
                        JsonSerializer.SerializeBytes(model);
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        JsonSerializer.SerializeNameless(stream, model);
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static Exception DeserializeException(ContentType contentType, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var model = ByteSerializer.Deserialize<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                case ContentType.Json:
                    {
                        var model = JsonSerializer.Deserialize<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                case ContentType.JsonNameless:
                    {
                        var model = JsonSerializer.DeserializeNameless<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static Task SerializeExceptionAsync(ContentType contentType, Stream stream, Exception ex)
        {
            var model = new ExceptionModel()
            {
                Message = ex.Message
            };

            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        return ByteSerializer.SerializeAsync(stream, model);
                    }
                case ContentType.Json:
                    {
                        return JsonSerializer.SerializeAsync(stream, model);
                    }
                case ContentType.JsonNameless:
                    {
                        return JsonSerializer.SerializeNamelessAsync(stream, model);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static async Task<Exception> DeserializeExceptionAsync(ContentType contentType, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var model = await ByteSerializer.DeserializeAsync<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                case ContentType.Json:
                    {
                        var model = await JsonSerializer.DeserializeAsync<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                case ContentType.JsonNameless:
                    {
                        var model = await JsonSerializer.DeserializeNamelessAsync<ExceptionModel>(stream);
                        return new Exception(model.Message);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}