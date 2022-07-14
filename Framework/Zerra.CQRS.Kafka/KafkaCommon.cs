// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.Kafka
{
    internal static class KafkaCommon
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

        public static async Task EnsureTopic(string host, string topic)
        {
            var adminClientConfig = new AdminClientConfig();
            adminClientConfig.BootstrapServers = host;

            using (var adminClient = new AdminClientBuilder(adminClientConfig).Build())
            {
                try
                {
                    var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
                    if (!metadata.Topics.Any(x => x.Topic == topic))
                    {
                        var topicSpecification = new TopicSpecification()
                        {
                            Name = topic,
                            ReplicationFactor = 1,
                            NumPartitions = 1
                        };
                        await adminClient.CreateTopicsAsync(new TopicSpecification[] { topicSpecification });
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(KafkaCommon)} failed to create topic {topic}", ex);
                }
            }
        }

        public static async Task DeleteTopic(string host, string topic)
        {
            var adminClientConfig = new AdminClientConfig();
            adminClientConfig.BootstrapServers = host;

            using (var adminClient = new AdminClientBuilder(adminClientConfig).Build())
            {
                try
                {
                    var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
                    if (metadata.Topics.Any(x => x.Topic == topic))
                    {
                        await adminClient.DeleteTopicsAsync(new string[] { topic });
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(KafkaCommon)} failed to delete topic {topic}", ex);
                }
            }
        }
    }
}
