// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus.Administration;

namespace Zerra.CQRS.AzureServiceBus
{
    internal static class AzureServiceBusCommon
    {
        private const int maxMessageSizeForPremium = 102400;

        private static readonly SemaphoreSlim locker = new(1, 1);
        private static readonly TimeSpan deleteWhenIdleTimeout = new(0, 5, 0);

        public const int EntityNameMaxLength = 50;

        public const int RetryDelay = 5000;

        public static async Task EnsureQueue(string host, string queue, bool deleteWhenIdle)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                var properties = await client.GetNamespacePropertiesAsync();
                var premium = properties.Value.MessagingSku == MessagingSku.Premium;
                var maxMessageSizeInKilobytes = premium ? maxMessageSizeForPremium : (int?)null;
                var autoDeleteOnIdle = deleteWhenIdle ? deleteWhenIdleTimeout : TimeSpan.MaxValue;

                if (!await client.QueueExistsAsync(queue))
                {
                    if (await client.TopicExistsAsync(queue))
                        _ = await client.DeleteTopicAsync(queue);

                    var options = new CreateQueueOptions(queue)
                    {
                        AutoDeleteOnIdle = autoDeleteOnIdle,
                        MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes
                    };
                    _ = await client.CreateQueueAsync(options);
                }
                else
                {
                    var existing = await client.GetQueueAsync(queue);
                    if (existing.Value.AutoDeleteOnIdle != autoDeleteOnIdle ||
                        existing.Value.MaxMessageSizeInKilobytes != maxMessageSizeForPremium)
                    {
                        existing.Value.AutoDeleteOnIdle = autoDeleteOnIdle;
                        existing.Value.MaxMessageSizeInKilobytes = maxMessageSizeForPremium;
                        _ = await client.UpdateQueueAsync(existing.Value);
                    }
                }
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task DeleteQueue(string host, string queue)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                if (await client.QueueExistsAsync(queue))
                    _ = await client.DeleteQueueAsync(queue);
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task EnsureTopic(string host, string topic, bool deleteWhenIdle)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                var properties = await client.GetNamespacePropertiesAsync();
                var premium = properties.Value.MessagingSku == MessagingSku.Premium;
                var maxMessageSizeInKilobytes = premium ? maxMessageSizeForPremium : (int?)null;
                var autoDeleteOnIdle = deleteWhenIdle ? deleteWhenIdleTimeout : TimeSpan.MaxValue;

                if (!await client.TopicExistsAsync(topic))
                {
                    if (await client.QueueExistsAsync(topic))
                        _ = await client.DeleteQueueAsync(topic);

                    var options = new CreateTopicOptions(topic)
                    {
                        AutoDeleteOnIdle = autoDeleteOnIdle,
                        MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes
                    };
                    _ = await client.CreateTopicAsync(options);
                }
                else
                {
                    var existing = await client.GetTopicAsync(topic);
                    if (existing.Value.AutoDeleteOnIdle != autoDeleteOnIdle ||
                        existing.Value.MaxMessageSizeInKilobytes != maxMessageSizeForPremium)
                    {
                        existing.Value.AutoDeleteOnIdle = autoDeleteOnIdle;
                        existing.Value.MaxMessageSizeInKilobytes = maxMessageSizeForPremium;
                        _ = await client.UpdateTopicAsync(existing.Value);
                    }
                }
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task DeleteTopic(string host, string topic)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                if (await client.TopicExistsAsync(topic))
                    _ = await client.DeleteTopicAsync(topic);
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task EnsureSubscription(string host, string topic, string subscription, bool deleteWhenIdle)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                if (!await client.SubscriptionExistsAsync(topic, subscription))
                {
                    var options = new CreateSubscriptionOptions(topic, subscription)
                    {
                        AutoDeleteOnIdle = deleteWhenIdle ? deleteWhenIdleTimeout : TimeSpan.MaxValue
                    };
                    _ = await client.CreateSubscriptionAsync(options);
                }
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task DeleteSubscription(string host, string topic, string subscription)
        {
            var client = new ServiceBusAdministrationClient(host);

            await locker.WaitAsync();
            try
            {
                if (await client.SubscriptionExistsAsync(topic, subscription))
                    _ = await client.DeleteSubscriptionAsync(topic, subscription);
            }
            finally
            {
                _ = locker.Release();
            }
        }

        //public static async Task DeleteAllAckTopics(string host)
        //{
        //    var client = new ServiceBusAdministrationClient(host);
        //    var topicPager = client.GetTopicsAsync();
        //    await foreach (var topic in topicPager)
        //    {
        //        var subcriptionPager = client.GetSubscriptionsAsync(topic.Name);
        //        await foreach (var subscription in subcriptionPager)
        //        {
        //            if (subscription.SubscriptionName.StartsWith("ACK-"))
        //                _ = await client.DeleteSubscriptionAsync(subscription.TopicName, subscription.SubscriptionName);
        //        }
        //        if (topic.Name.StartsWith("ACK-"))
        //            _ = await client.DeleteTopicAsync(topic.Name);
        //    }
        //}
    }
}
