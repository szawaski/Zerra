// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using RabbitMQ.Client;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.RabbitMQ
{
    internal static class RabbitMQCommon
    {
        public const int TopicMaxLength = 255;

        public const int RetryDelay = 5000;

        //an AMQP URI (amqp://user:password@host:port/vhost, amqps:// for TLS) configures the whole connection, otherwise the value is only the host name
        public static ConnectionFactory CreateConnectionFactory(string host)
        {
            var factory = new ConnectionFactory();
            if (host.Contains("://"))
                factory.Uri = new Uri(host);
            else
                factory.HostName = host;
            return factory;
        }

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj);
        }

        public static object? Deserialize(byte[] data, Type type)
        {
            return ByteSerializer.Deserialize(data, type);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes);
        }
    }
}
