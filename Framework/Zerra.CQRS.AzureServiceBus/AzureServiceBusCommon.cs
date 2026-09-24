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

        private const int emulatorAdministrationPort = 5300;

        //the Service Bus emulator only serves the administration API on its management port, while the connection string's endpoint is its AMQP port,
        //so for the emulator the administration client gets the same connection string pointed at the management port
        public static ServiceBusAdministrationClient CreateAdministrationClient(string host, ServiceBusAdministrationClientOptions? options = null)
        {
#if NETSTANDARD2_0
            var parts = host.Split([';'], StringSplitOptions.RemoveEmptyEntries);
#else
            var parts = host.Split(';', StringSplitOptions.RemoveEmptyEntries);
#endif

            var isEmulator = false;
            var endpointIndex = -1;
            for (var i = 0; i < parts.Length; i++)
            {
                var separator = parts[i].IndexOf('=');
                if (separator < 0)
                    continue;
                var key = parts[i].AsSpan(0, separator).Trim();
                if (key.Equals("UseDevelopmentEmulator", StringComparison.OrdinalIgnoreCase))
#if NETSTANDARD2_0
                    isEmulator = bool.TryParse(parts[i].Substring(separator + 1).Trim(), out var value) && value;
#else
                    isEmulator = bool.TryParse(parts[i].AsSpan(separator + 1).Trim(), out var value) && value;
#endif
                else if (key.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                    endpointIndex = i;
            }

            if (!isEmulator || endpointIndex < 0)
                return new ServiceBusAdministrationClient(host, options ?? new());

            var endpointPart = parts[endpointIndex];
            var endpoint = new UriBuilder(endpointPart.Substring(endpointPart.IndexOf('=') + 1).Trim()) { Port = emulatorAdministrationPort };
            parts[endpointIndex] = $"Endpoint={endpoint.Uri}";
            return new ServiceBusAdministrationClient(String.Join(";", parts), options ?? new());
        }

        public static async Task EnsureQueue(string host, string queue, bool deleteWhenIdle)
        {
            var client = CreateAdministrationClient(host);

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
            var client = CreateAdministrationClient(host);

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
            var client = CreateAdministrationClient(host);

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
            var client = CreateAdministrationClient(host);

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
            var client = CreateAdministrationClient(host);

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
            var client = CreateAdministrationClient(host);

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
