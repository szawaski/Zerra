// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Azure.Management.EventHub;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureEventHub
{
    internal static class AzureEventHubCommon
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            UsePropertyNames = true,
            IncludePropertyTypes = true,
            IgnoreIndexAttribute = true
        };

        private static readonly SemaphoreSlim locker = new(1, 1);

        public const int RetryDelay = 5000;

        private const string resourceGroupConfig = "AzureEventHubResourceGroup";
        private const string tenantIDConfig = "AzureEventHubTenantId";
        private const string applicationIDConfig = "AzureEventHubApplicationId";
        private const string clientSecretConfig = "AzureEventHubClientSecret";
        private const string subscriptionIDConfig = "AzureEventHubSubscriptionId";

        public const string AckProperty = "Ack";
        public const string AckKeyProperty = "AckID";
        public const string TypeProperty = "Type";
        public const string EnvironmentProperty = "Env";

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj, byteSerializerOptions);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions);
        }

        public static async Task<string> GetEnsuredConsumerGroup(string requestedConsumerGroup, string connectionString, string eventHubName)
        {
            var resourceGroup = Config.GetSetting(resourceGroupConfig);
            var tenantID = Config.GetSetting(tenantIDConfig);
            var applicationID = Config.GetSetting(applicationIDConfig);
            var clientSecret = Config.GetSetting(clientSecretConfig);
            var subscriptionID = Config.GetSetting(subscriptionIDConfig);

            if (String.IsNullOrWhiteSpace(resourceGroup) ||
                String.IsNullOrWhiteSpace(tenantID) ||
                String.IsNullOrWhiteSpace(applicationID) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                String.IsNullOrWhiteSpace(subscriptionID))
            {
                return EventHubConsumerClient.DefaultConsumerGroupName;
            }

            await locker.WaitAsync();
            try
            {
                string hubNamespace = null;
                var split = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in split)
                {
                    var itemSplit = item.Split('=');
                    if (itemSplit.Length != 2)
                        continue;
                    if (itemSplit[0] == "Endpoint")
                    {
                        var uri = new Uri(itemSplit[1]);
                        hubNamespace = uri.Host.Split('.')[0];
                    }
                }

                if (hubNamespace == null)
                    throw new Exception($"{nameof(GetEnsuredConsumerGroup)} could not parse host");

                var context = new AuthenticationContext($"https://login.windows.net/{tenantID}");
                var credential = new ClientCredential(applicationID, clientSecret);
                var token = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);

                var clientCredentials = new TokenCredentials(token.AccessToken);
                var client = new EventHubManagementClient(clientCredentials)
                {
                    SubscriptionId = subscriptionID,
                };

                var consumerGroupResponse = await client.ConsumerGroups
                    .CreateOrUpdateAsync(resourceGroup, hubNamespace, eventHubName, requestedConsumerGroup);

                return requestedConsumerGroup;
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public static async Task DeleteConsumerGroup(string requestedConsumerGroup, string connectionString, string eventHubName)
        {
            var resourceGroup = Config.GetSetting(resourceGroupConfig);
            var tenantID = Config.GetSetting(tenantIDConfig);
            var applicationID = Config.GetSetting(applicationIDConfig);
            var clientSecret = Config.GetSetting(clientSecretConfig);
            var subscriptionID = Config.GetSetting(subscriptionIDConfig);

            if (String.IsNullOrWhiteSpace(resourceGroup) ||
                String.IsNullOrWhiteSpace(tenantID) ||
                String.IsNullOrWhiteSpace(applicationID) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                String.IsNullOrWhiteSpace(subscriptionID))
            {
                return;
            }

            await locker.WaitAsync();
            try
            {
                string hubNamespace = null;
                var split = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in split)
                {
                    var itemSplit = item.Split('=');
                    if (itemSplit.Length != 2)
                        continue;
                    if (itemSplit[0] == "Endpoint")
                    {
                        var uri = new Uri(itemSplit[1]);
                        hubNamespace = uri.Host.Split('.')[0];
                    }
                }

                if (hubNamespace == null)
                    throw new Exception($"{nameof(GetEnsuredConsumerGroup)} could not parse host");

                var context = new AuthenticationContext($"https://login.windows.net/{tenantID}");
                var credential = new ClientCredential(applicationID, clientSecret);
                var token = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);

                var clientCredentials = new TokenCredentials(token.AccessToken);
                var client = new EventHubManagementClient(clientCredentials)
                {
                    SubscriptionId = subscriptionID,
                };

                await client.ConsumerGroups
                    .DeleteAsync(resourceGroup, hubNamespace, eventHubName, requestedConsumerGroup);
            }
            finally
            {
                _ = locker.Release();
            }
        }
    }
}
