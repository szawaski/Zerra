// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using Zerra.Reflection;
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
                        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
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
                        return System.Text.Json.JsonSerializer.Deserialize<T>(bytes);
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
                        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
#if NETSTANDARD2_0
                        stream.Write(bytes, 0, bytes.Length);
#else
                        stream.Write(bytes.AsSpan());
#endif
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        var bytes = JsonSerializer.SerializeNamelessBytes(obj);
#if NETSTANDARD2_0
                        stream.Write(bytes, 0, bytes.Length);
#else
                        stream.Write(bytes.AsSpan());
#endif
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
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return System.Text.Json.JsonSerializer.Deserialize<T>(ms.ToArray());
                        }
                    }
                case ContentType.JsonNameless:
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return JsonSerializer.DeserializeNameless<T>(ms.ToArray());
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static async Task SerializeAsync(ContentType contentType, Stream stream, object obj)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, false, true);
                        await serializer.SerializeAsync(stream, obj);
                        return;
                    }
                case ContentType.Json:
                    {
                        await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj);
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        var bytes = JsonSerializer.SerializeNamelessBytes(obj);
#if NETSTANDARD2_0
                        await stream.WriteAsync(bytes, 0, bytes.Length);
#else
                        await stream.WriteAsync(bytes.AsMemory());
#endif
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        public static async Task<T> DeserializeAsync<T>(ContentType contentType, Stream stream)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, false, true);
                        return await serializer.DeserializeAsync<T>(stream);
                    }
                case ContentType.Json:
                    {
                        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream);
                    }
                case ContentType.JsonNameless:
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            return JsonSerializer.DeserializeNameless<T>(ms.ToArray());
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static void SerializeException(ContentType contentType, Stream stream, Exception ex)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, true, true);
                        serializer.Serialize(stream, ex);
                        return;
                    }
                case ContentType.Json:
                    {
                        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(ex);
#if NETSTANDARD2_0
                        stream.Write(bytes, 0, bytes.Length);
#else
                        stream.Write(bytes.AsSpan());
#endif
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        JsonSerializer.SerializeNameless(stream, ex);
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
                        var serializer = new ByteSerializer(true, true, true);
                        return serializer.Deserialize<Exception>(stream);
                    }
                case ContentType.Json:
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return System.Text.Json.JsonSerializer.Deserialize<Exception>(ms.ToArray());
                        }
                    }
                case ContentType.JsonNameless:
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return JsonSerializer.DeserializeNameless<Exception>(ms.ToArray());
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static async Task SerializeExceptionAsync(ContentType contentType, Stream stream, Exception ex)
        {
            switch (contentType)
            {
                case ContentType.Bytes:
                    {
                        var serializer = new ByteSerializer(true, true, true);
                        await serializer.SerializeAsync(stream, ex);
                        return;
                    }
                case ContentType.Json:
                    {
                        await System.Text.Json.JsonSerializer.SerializeAsync(stream, ex);
                        return;
                    }
                case ContentType.JsonNameless:
                    {
                        var bytes = JsonSerializer.SerializeNamelessBytes(ex);
#if NETSTANDARD2_0
                        stream.Write(bytes, 0, bytes.Length);
#else
                        stream.Write(bytes.AsSpan());
#endif
                        return;
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
                        var serializer = new ByteSerializer(true, true, true);
                        return await serializer.DeserializeAsync<Exception>(stream);
                    }
                case ContentType.Json:
                    {
                        return await System.Text.Json.JsonSerializer.DeserializeAsync<Exception>(stream);
                    }
                case ContentType.JsonNameless:
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            return JsonSerializer.DeserializeNameless<Exception>(ms.ToArray());
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}