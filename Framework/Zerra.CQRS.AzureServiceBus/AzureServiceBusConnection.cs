// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Threading.Tasks;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    /// <summary>
    /// Checks whether an Azure Service Bus namespace can be reached, such as at startup to choose between Service Bus and a direct transport.
    /// </summary>
    public static class AzureServiceBusConnection
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Tests the connection by reading the namespace's properties, the same administration endpoint that creates queues and topics.
        /// </summary>
        /// <param name="host">The Azure Service Bus connection string.</param>
        /// <param name="timeout">How long to wait for the namespace, five seconds if not given.</param>
        /// <returns>True if the namespace answered; otherwise false.</returns>
        public static async Task<bool> TestAsync(string host, TimeSpan? timeout = null)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            try
            {
                //no retries, an unreachable namespace should fail within the timeout rather than after the default retry backoff
                var options = new ServiceBusAdministrationClientOptions();
                options.Retry.MaxRetries = 0;
                options.Retry.NetworkTimeout = timeout ?? defaultTimeout;

                var client = AzureServiceBusCommon.CreateAdministrationClient(host, options);
                _ = await client.GetNamespacePropertiesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _ = Log.WarnAsync($"{nameof(AzureServiceBusConnection)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
