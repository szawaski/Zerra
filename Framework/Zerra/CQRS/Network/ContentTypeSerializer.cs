// Copyright © KaKush LLC
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
        public static byte[] Serialize(ContentType contentType, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, false, true);
                        return serializer.Serialize(obj);
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
                        var serializer = new ByteSerializer(true, false, true);
                        return serializer.Deserialize<T>(bytes);
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

        public static void Serialize(ContentType contentType, Stream stream, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, false, true);
                        serializer.Serialize(stream, obj);
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
                        var serializer = new ByteSerializer(true, false, true);
                        return serializer.Deserialize<T>(stream);
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

        public static Task SerializeAsync(ContentType contentType, Stream stream, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, false, true);
                        return serializer.SerializeAsync(stream, obj);
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
                        var serializer = new ByteSerializer(true, false, true);
                        return serializer.DeserializeAsync<T>(stream);
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
                        var serializer = new ByteSerializer();
                        serializer.Serialize(stream, model);
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
                        var serializer = new ByteSerializer();
                        var model = serializer.Deserialize<ExceptionModel>(stream);
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
                        var serializer = new ByteSerializer();
                        return serializer.SerializeAsync(stream, model);
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
                        var serializer = new ByteSerializer();
                        var model = await serializer.DeserializeAsync<ExceptionModel>(stream);
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