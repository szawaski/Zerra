// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus.Administration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    internal static class AzureServiceBusCommon
    {
        public const int RetryDelay = 10000;

        public static byte[] Serialize(object obj)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.Serialize(obj);
        }

        public static Task<T> DeserializeAsync<T>(Stream stream)
        {
            var serializer = new ByteSerializer(true, true, true);
            return serializer.DeserializeAsync<T>(stream);
        }

        public static async Task EnsureTopic(string host, string topic)
        {
            var adminClient = new ServiceBusAdministrationClient(host);
            if (!await adminClient.TopicExistsAsync(topic))
                _ = await adminClient.CreateTopicAsync(topic);
        }

        public static async Task DeleteTopic(string host, string topic)
        {
            var adminClient = new ServiceBusAdministrationClient(host);
            if (await adminClient.TopicExistsAsync(topic))
                _ = await adminClient.DeleteTopicAsync(topic);
        }
    }
}
