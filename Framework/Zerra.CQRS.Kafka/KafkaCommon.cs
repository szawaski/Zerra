﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS.Kafka
{
    internal static class KafkaCommon
    {
        public const string MessageKey = "Body";
        public const string MessageWithAckKey = "BodyAck";
        public const string AckKeyHeader = "AckKey";

        public static byte[] Serialize(object obj)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.Serialize(obj);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.Deserialize<T>(bytes);
        }
    }
}