// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Azure.Management.EventHub;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureEventHub
{
    internal static class AzureEventHubCommon
    {
        public const int RetryDelay = 10000;

        public const string AckProperty = "Ack";
        public const string AckKeyProperty = "AckID";
        public const string TypeProperty= "Type";

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

        public static async Task<string> GetEnsuredConsumerGroup(string requestedConsumerGroup, string connectionString, string eventHubName)
        {
            var resourceGroup = Config.GetSetting("AzureEventHubResourceGroup");
            var tenantID = Config.GetSetting("AzureEventHubTenantId");
            var applicationID = Config.GetSetting("AzureEventHubApplicationId");
            var clientSecret = Config.GetSetting("AzureEventHubClientSecret");
            var subscriptionID = Config.GetSetting("AzureEventHubSubscriptionId");

            if (String.IsNullOrWhiteSpace(resourceGroup) ||
                String.IsNullOrWhiteSpace(tenantID) ||
                String.IsNullOrWhiteSpace(applicationID) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                String.IsNullOrWhiteSpace(subscriptionID))
            {
                return EventHubConsumerClient.DefaultConsumerGroupName;
            }

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

            var serviceClientCredentials = new TokenCredentials(token.AccessToken);
            var eventHubManagementClient = new EventHubManagementClient(serviceClientCredentials)
            {
                SubscriptionId = subscriptionID,
            };

            var consumerGroupResponse = await eventHubManagementClient.ConsumerGroups
                .CreateOrUpdateAsync(resourceGroup, hubNamespace, eventHubName, requestedConsumerGroup);

            return requestedConsumerGroup;
        }

        public static async Task DeleteConsumerGroup(string requestedConsumerGroup, string connectionString, string eventHubName)
        {
            var resourceGroup = Config.GetSetting("AzureEventHubResourceGroup");
            var tenantID = Config.GetSetting("AzureEventHubTenantId");
            var applicationID = Config.GetSetting("AzureEventHubApplicationId");
            var clientSecret = Config.GetSetting("AzureEventClientSecret");
            var subscriptionID = Config.GetSetting("AzureEventHubSubscriptionId");

            if (String.IsNullOrWhiteSpace(resourceGroup) ||
                String.IsNullOrWhiteSpace(tenantID) ||
                String.IsNullOrWhiteSpace(applicationID) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                String.IsNullOrWhiteSpace(subscriptionID))
            {
                return;
            }

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

            var serviceClientCredentials = new TokenCredentials(token.AccessToken);
            var eventHubManagementClient = new EventHubManagementClient(serviceClientCredentials)
            {
                SubscriptionId = subscriptionID,
            };

            await eventHubManagementClient.ConsumerGroups
                .DeleteAsync(resourceGroup, hubNamespace, eventHubName, requestedConsumerGroup);
        }
    }
}
