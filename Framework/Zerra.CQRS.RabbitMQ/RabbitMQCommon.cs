// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    internal static class RabbitMQCommon
    {
        public const int TopicMaxLength = 255;

        public static byte[] Serialize(object obj)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.Serialize(obj);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.Deserialize<T>(bytes);
        }
    }
}
