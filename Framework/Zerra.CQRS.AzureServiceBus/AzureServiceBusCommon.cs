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
        public const int RetryDelay = 5000;

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
            var client = new ServiceBusAdministrationClient(host);
            if (!await client.TopicExistsAsync(topic))
                _ = await client.CreateTopicAsync(topic);
        }

        public static async Task DeleteTopic(string host, string topic)
        {
            var client = new ServiceBusAdministrationClient(host);
            if (await client.TopicExistsAsync(topic))
                _ = await client.DeleteTopicAsync(topic);
        }

        public static async Task EnsureSubscription(string host, string topic, string subscription)
        {
            var client = new ServiceBusAdministrationClient(host);
            if (!await client.SubscriptionExistsAsync(topic, subscription))
                _ = await client.CreateSubscriptionAsync(topic, subscription);
        }

        public static async Task DeleteSubscription(string host, string topic, string subscription)
        {
            var client = new ServiceBusAdministrationClient(host);
            if (await client.SubscriptionExistsAsync(topic, subscription))
                _ = await client.DeleteSubscriptionAsync(topic, subscription);
        }

        public static async Task DeleteAllAckTopics(string host)
        {
            var client = new ServiceBusAdministrationClient(host);
            var topicPager = client.GetTopicsAsync();
            await foreach (var topic in topicPager)
            {
                var subcriptionPager = client.GetSubscriptionsAsync(topic.Name);
                await foreach (var subscription in subcriptionPager)
                {
                    if (subscription.SubscriptionName.StartsWith("ACK-"))
                        _ = await client.DeleteSubscriptionAsync(subscription.TopicName, subscription.SubscriptionName);
                }
                if (topic.Name.StartsWith("ACK-"))
                    _ = await client.DeleteTopicAsync(topic.Name);
            }
        }
    }
}
