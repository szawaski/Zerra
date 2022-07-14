// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureEventGrid
{
    internal static class AzureEventGridCommon
    {
        public const int RetryDelay = 10000;

        public const string MessageKey = "Body";
        public const string MessageWithAckKey = "BodyAck";
        public const string AckTopicHeader = "AckTopic";
        public const string AckKeyHeader = "AckKey";

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

        public static async Task EnsureTopic(string topicName)
        {
            var resourceGroup = Config.GetSetting("AzureEventGridResourceGroup");
            var tenantID = Config.GetSetting("AzureEventGridTenantId");
            var applicationID = Config.GetSetting("AzureEventGridApplicationId");
            var clientSecret = Config.GetSetting("AzureEventGridClientSecret");
            var subscriptionID = Config.GetSetting("AzureEventGridSubscriptionId");


            var context = new AuthenticationContext($"https://login.windows.net/{tenantID}");
            var credential = new ClientCredential(applicationID, clientSecret);
            var token = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);

            var clientCredentials = new TokenCredentials(token.AccessToken);
            var client = new EventGridManagementClient(clientCredentials)
            {
                SubscriptionId = subscriptionID,
            };

            var topic = new Topic();
            _ = await client.Topics.CreateOrUpdateAsync(resourceGroup, topicName, topic);
        }

        public static async Task DeleteTopic(string topicName)
        {
            var resourceGroup = Config.GetSetting("AzureEventGridResourceGroup");
            var tenantID = Config.GetSetting("AzureEventGridTenantId");
            var applicationID = Config.GetSetting("AzureEventGridApplicationId");
            var clientSecret = Config.GetSetting("AzureEventGridClientSecret");
            var subscriptionID = Config.GetSetting("AzureEventGridSubscriptionId");


            var context = new AuthenticationContext($"https://login.windows.net/{tenantID}");
            var credential = new ClientCredential(applicationID, clientSecret);
            var token = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);

            var clientCredentials = new TokenCredentials(token.AccessToken);
            var client = new EventGridManagementClient(clientCredentials)
            {
                SubscriptionId = subscriptionID,
            };

            await client.Topics.DeleteAsync(resourceGroup, topicName);
        }
    }
}
