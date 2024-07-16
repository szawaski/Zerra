// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.RabbitMQ
{
    internal static class RabbitMQCommon
    {
        public const int TopicMaxLength = 255;

        public const int RetryDelay = 5000;

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj);
        }

        public static object? Deserialize(Type type, byte[] data)
        {
            return ByteSerializer.Deserialize(type, data);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes);
        }
    }
}
