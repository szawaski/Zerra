// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Zerra.CQRS.AzureServiceBus
{
    internal static class AzureServiceBusCommon
    {
        private const int maxMessageSizeForPremium = 102400;

        //also how long a consumer keeps a reply sender to an acknowledgement queue that hasn't been used
        public static readonly TimeSpan DeleteWhenIdleTimeout = new(0, 5, 0);

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

        private static readonly ConcurrentDictionary<string, AzureServiceBusCommonNamespace> namespaces = new();

        public static AzureServiceBusCommonNamespace GetNamespace(string host) => namespaces.TryGetValue(host, out var serviceBusNamespace) ? serviceBusNamespace : namespaces.GetOrAdd(host, static (host) => new AzureServiceBusCommonNamespace(host));

        private static Dictionary<string, TValue> With<TValue>(Dictionary<string, TValue> source, string key, TValue value)
        {
            var copy = new Dictionary<string, TValue>(source, StringComparer.OrdinalIgnoreCase);
            copy[key] = value;
            return copy;
        }
        private static Dictionary<string, TValue> Without<TValue>(Dictionary<string, TValue> source, string key)
        {
            if (!source.ContainsKey(key))
                return source;
            var copy = new Dictionary<string, TValue>(source, StringComparer.OrdinalIgnoreCase);
            _ = copy.Remove(key);
            return copy;
        }

        //the message size can only be set on Premium, elsewhere it never matches and every start would update the queue or topic
        private static bool Matches(AzureServiceBusCommonNamespace.EntitySettings settings, TimeSpan autoDeleteOnIdle, bool premium)
            => settings.AutoDeleteOnIdle == autoDeleteOnIdle && (!premium || settings.MaxMessageSizeInKilobytes == maxMessageSizeForPremium);

        //returns before its first await when the queue is known, which completes without a Task
        public static async ValueTask EnsureQueue(AzureServiceBusCommonNamespace serviceBusNamespace, string queue, bool deleteWhenIdle)
        {
            var queues = serviceBusNamespace.Queues;
            if (queues is not null && queues.TryGetValue(queue, out var settings) && Matches(settings, deleteWhenIdle ? DeleteWhenIdleTimeout : TimeSpan.MaxValue, serviceBusNamespace.Premium))
                return;

            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                await serviceBusNamespace.Load();

                var autoDeleteOnIdle = deleteWhenIdle ? DeleteWhenIdleTimeout : TimeSpan.MaxValue;
                var maxMessageSizeInKilobytes = serviceBusNamespace.Premium ? maxMessageSizeForPremium : (int?)null;

                if (!serviceBusNamespace.Queues!.TryGetValue(queue, out var existing))
                {
                    if (serviceBusNamespace.Topics!.ContainsKey(queue))
                    {
                        _ = await serviceBusNamespace.Client.DeleteTopicAsync(queue);
                        serviceBusNamespace.Topics = Without(serviceBusNamespace.Topics, queue);
                        serviceBusNamespace.SubscriptionsByTopic = Without(serviceBusNamespace.SubscriptionsByTopic, queue);
                    }

                    var options = new CreateQueueOptions(queue)
                    {
                        AutoDeleteOnIdle = autoDeleteOnIdle,
                        MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes
                    };
                    QueueProperties created;
                    try
                    {
                        created = await serviceBusNamespace.Client.CreateQueueAsync(options);
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                    {
                        //another service created it first
                        created = await serviceBusNamespace.Client.GetQueueAsync(queue);
                    }
                    serviceBusNamespace.Queues = With(serviceBusNamespace.Queues, queue, new AzureServiceBusCommonNamespace.EntitySettings(created.AutoDeleteOnIdle, created.MaxMessageSizeInKilobytes));
                }
                else if (!Matches(existing, autoDeleteOnIdle, serviceBusNamespace.Premium))
                {
                    QueueProperties properties = await serviceBusNamespace.Client.GetQueueAsync(queue);
                    properties.AutoDeleteOnIdle = autoDeleteOnIdle;
                    if (maxMessageSizeInKilobytes.HasValue)
                        properties.MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes;
                    QueueProperties updated = await serviceBusNamespace.Client.UpdateQueueAsync(properties);
                    serviceBusNamespace.Queues = With(serviceBusNamespace.Queues, queue, new AzureServiceBusCommonNamespace.EntitySettings(updated.AutoDeleteOnIdle, updated.MaxMessageSizeInKilobytes));
                }
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        public static Task DeleteQueue(string host, string queue) => DeleteQueue(GetNamespace(host), queue);

        public static async Task DeleteQueue(AzureServiceBusCommonNamespace serviceBusNamespace, string queue)
        {
            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                if (await serviceBusNamespace.Client.QueueExistsAsync(queue))
                    _ = await serviceBusNamespace.Client.DeleteQueueAsync(queue);
                if (serviceBusNamespace.Queues is not null)
                    serviceBusNamespace.Queues = Without(serviceBusNamespace.Queues, queue);
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        //returns before its first await when the topic is known, which completes without a Task
        public static async ValueTask EnsureTopic(AzureServiceBusCommonNamespace serviceBusNamespace, string topic, bool deleteWhenIdle)
        {
            var topics = serviceBusNamespace.Topics;
            if (serviceBusNamespace.Queues is not null && topics is not null && topics.TryGetValue(topic, out var settings) && Matches(settings, deleteWhenIdle ? DeleteWhenIdleTimeout : TimeSpan.MaxValue, serviceBusNamespace.Premium))
                return;

            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                await serviceBusNamespace.Load();

                var autoDeleteOnIdle = deleteWhenIdle ? DeleteWhenIdleTimeout : TimeSpan.MaxValue;
                var maxMessageSizeInKilobytes = serviceBusNamespace.Premium ? maxMessageSizeForPremium : (int?)null;

                if (!serviceBusNamespace.Topics!.TryGetValue(topic, out var existing))
                {
                    if (serviceBusNamespace.Queues!.ContainsKey(topic))
                    {
                        _ = await serviceBusNamespace.Client.DeleteQueueAsync(topic);
                        serviceBusNamespace.Queues = Without(serviceBusNamespace.Queues, topic);
                    }

                    var options = new CreateTopicOptions(topic)
                    {
                        AutoDeleteOnIdle = autoDeleteOnIdle,
                        MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes
                    };
                    TopicProperties created;
                    try
                    {
                        created = await serviceBusNamespace.Client.CreateTopicAsync(options);
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                    {
                        //another service created it first
                        created = await serviceBusNamespace.Client.GetTopicAsync(topic);
                    }
                    serviceBusNamespace.Topics = With(serviceBusNamespace.Topics, topic, new AzureServiceBusCommonNamespace.EntitySettings(created.AutoDeleteOnIdle, created.MaxMessageSizeInKilobytes));
                }
                else if (!Matches(existing, autoDeleteOnIdle, serviceBusNamespace.Premium))
                {
                    TopicProperties properties = await serviceBusNamespace.Client.GetTopicAsync(topic);
                    properties.AutoDeleteOnIdle = autoDeleteOnIdle;
                    if (maxMessageSizeInKilobytes.HasValue)
                        properties.MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes;
                    TopicProperties updated = await serviceBusNamespace.Client.UpdateTopicAsync(properties);
                    serviceBusNamespace.Topics = With(serviceBusNamespace.Topics, topic, new AzureServiceBusCommonNamespace.EntitySettings(updated.AutoDeleteOnIdle, updated.MaxMessageSizeInKilobytes));
                }
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        public static Task DeleteTopic(string host, string topic) => DeleteTopic(GetNamespace(host), topic);

        public static async Task DeleteTopic(AzureServiceBusCommonNamespace serviceBusNamespace, string topic)
        {
            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                if (await serviceBusNamespace.Client.TopicExistsAsync(topic))
                    _ = await serviceBusNamespace.Client.DeleteTopicAsync(topic);
                if (serviceBusNamespace.Topics is not null)
                    serviceBusNamespace.Topics = Without(serviceBusNamespace.Topics, topic);
                serviceBusNamespace.SubscriptionsByTopic = Without(serviceBusNamespace.SubscriptionsByTopic, topic);
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        //returns before its first await when the subscription is known, which completes without a Task
        public static async ValueTask EnsureSubscription(AzureServiceBusCommonNamespace serviceBusNamespace, string topic, string subscription, bool deleteWhenIdle)
        {
            if (serviceBusNamespace.SubscriptionsByTopic.TryGetValue(topic, out var known) && known.Contains(subscription))
                return;

            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                if (!serviceBusNamespace.SubscriptionsByTopic.TryGetValue(topic, out var subscriptions))
                {
                    subscriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    await foreach (var existing in serviceBusNamespace.Client.GetSubscriptionsAsync(topic))
                        _ = subscriptions.Add(existing.SubscriptionName);
                    serviceBusNamespace.SubscriptionsByTopic = With(serviceBusNamespace.SubscriptionsByTopic, topic, subscriptions);
                }
                if (subscriptions.Contains(subscription))
                    return;

                var options = new CreateSubscriptionOptions(topic, subscription)
                {
                    AutoDeleteOnIdle = deleteWhenIdle ? DeleteWhenIdleTimeout : TimeSpan.MaxValue
                };
                try
                {
                    _ = await serviceBusNamespace.Client.CreateSubscriptionAsync(options);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    //another replica created it first
                }
                serviceBusNamespace.SubscriptionsByTopic = With(serviceBusNamespace.SubscriptionsByTopic, topic, new HashSet<string>(subscriptions, StringComparer.OrdinalIgnoreCase) { subscription });
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        public static Task DeleteSubscription(string host, string topic, string subscription) => DeleteSubscription(GetNamespace(host), topic, subscription);

        public static async Task DeleteSubscription(AzureServiceBusCommonNamespace serviceBusNamespace, string topic, string subscription)
        {
            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                if (await serviceBusNamespace.Client.SubscriptionExistsAsync(topic, subscription))
                    _ = await serviceBusNamespace.Client.DeleteSubscriptionAsync(topic, subscription);
                if (serviceBusNamespace.SubscriptionsByTopic.TryGetValue(topic, out var subscriptions) && subscriptions.Contains(subscription))
                {
                    var remaining = new HashSet<string>(subscriptions, StringComparer.OrdinalIgnoreCase);
                    _ = remaining.Remove(subscription);
                    serviceBusNamespace.SubscriptionsByTopic = With(serviceBusNamespace.SubscriptionsByTopic, topic, remaining);
                }
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
            }
        }

        //an entity deleted outside of this process is still in the lists, a consumer that fails forgets it so the next ensure checks the namespace again
        public static async Task Forget(AzureServiceBusCommonNamespace serviceBusNamespace, string queueOrTopic)
        {
            await serviceBusNamespace.Locker.WaitAsync();
            try
            {
                if (serviceBusNamespace.Queues is not null)
                    serviceBusNamespace.Queues = Without(serviceBusNamespace.Queues, queueOrTopic);
                if (serviceBusNamespace.Topics is not null)
                    serviceBusNamespace.Topics = Without(serviceBusNamespace.Topics, queueOrTopic);
                serviceBusNamespace.SubscriptionsByTopic = Without(serviceBusNamespace.SubscriptionsByTopic, queueOrTopic);
            }
            finally
            {
                _ = serviceBusNamespace.Locker.Release();
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
