// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.AzureServiceBus
{
    /// <summary>
    /// One administration client per namespace for the process, and its queues, topics, and subscriptions, listed once and then kept by <see cref="AzureServiceBusCommon"/>.
    /// Each namespace has its own lock, so calls to one don't wait on another. The lists are replaced rather than changed, under the lock, so they can be read without it.
    /// Producers and consumers get theirs once from <see cref="AzureServiceBusCommon.GetNamespace"/>.
    /// </summary>
    internal sealed class AzureServiceBusCommonNamespace
    {
        //the settings compared when a queue or topic is ensured
        public readonly struct EntitySettings
        {
            public readonly TimeSpan AutoDeleteOnIdle;
            public readonly long? MaxMessageSizeInKilobytes;
            public EntitySettings(TimeSpan autoDeleteOnIdle, long? maxMessageSizeInKilobytes)
            {
                AutoDeleteOnIdle = autoDeleteOnIdle;
                MaxMessageSizeInKilobytes = maxMessageSizeInKilobytes;
            }
        }

        public readonly SemaphoreSlim Locker = new(1, 1);
        public readonly ServiceBusAdministrationClient Client;
        public bool Premium;
        private volatile bool premiumLoaded;
        //names are case insensitive, and listed in lower case, Queues is set last so a namespace with Queues is loaded
        public volatile Dictionary<string, EntitySettings>? Queues;
        public volatile Dictionary<string, EntitySettings>? Topics;
        //listed per topic the first time one of its subscriptions is ensured
        public volatile Dictionary<string, HashSet<string>> SubscriptionsByTopic = new(StringComparer.OrdinalIgnoreCase);

        public AzureServiceBusCommonNamespace(string host) => Client = AzureServiceBusCommon.CreateAdministrationClient(host);

        //called under the lock
        public async Task Load()
        {
            if (Queues is not null)
                return;
            _ = await GetPremium();
            var queues = new Dictionary<string, EntitySettings>(StringComparer.OrdinalIgnoreCase);
            await foreach (var queue in Client.GetQueuesAsync())
                queues[queue.Name] = new EntitySettings(queue.AutoDeleteOnIdle, queue.MaxMessageSizeInKilobytes);
            var topics = new Dictionary<string, EntitySettings>(StringComparer.OrdinalIgnoreCase);
            await foreach (var topic in Client.GetTopicsAsync())
                topics[topic.Name] = new EntitySettings(topic.AutoDeleteOnIdle, topic.MaxMessageSizeInKilobytes);
            Topics = topics;
            Queues = queues;
        }

        //doesn't need the lock, two callers fetching it at once get the same answer
        public async ValueTask<bool> GetPremium()
        {
            if (!premiumLoaded)
            {
                Premium = (await Client.GetNamespacePropertiesAsync()).Value.MessagingSku == MessagingSku.Premium;
                premiumLoaded = true;
            }
            return Premium;
        }
    }
}
