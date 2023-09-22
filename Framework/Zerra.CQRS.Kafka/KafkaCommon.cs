// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.CQRS.Kafka
{
    internal static class KafkaCommon
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new ByteSerializerOptions()
        {
            UsePropertyNames = true,
            IncludePropertyTypes = true,
            IgnoreIndexAttribute = true
        };

        private static readonly SemaphoreSlim locker = new(1, 1);

        public const int TopicMaxLength = 249;

        public const int RetryDelay = 5000;

        public const string MessageKey = "Body";
        public const string MessageWithAckKey = "BodyAck";
        public const string AckTopicHeader = "AckTopic";
        public const string AckKeyHeader = "AckKey";

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj, byteSerializerOptions);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions);
        }

        public static async Task EnsureTopic(string host, string topic)
        {
            var clientConfig = new AdminClientConfig();
            clientConfig.BootstrapServers = host;

            await locker.WaitAsync();
            try
            {
                using (var client = new AdminClientBuilder(clientConfig).Build())
                {
                    try
                    {
                        var metadata = client.GetMetadata(topic, TimeSpan.FromSeconds(10));
                        if (!metadata.Topics.Any(x => x.Topic == topic))
                        {
                            var topicSpecification = new TopicSpecification()
                            {
                                Name = topic,
                                ReplicationFactor = 1,
                                NumPartitions = 1
                            };
                            await client.CreateTopicsAsync(new TopicSpecification[] { topicSpecification });
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{nameof(KafkaCommon)} failed to create topic {topic}", ex);
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
            var clientConfig = new AdminClientConfig();
            clientConfig.BootstrapServers = host;


            await locker.WaitAsync();
            try
            {
                using (var client = new AdminClientBuilder(clientConfig).Build())
                {
                    try
                    {
                        var metadata = client.GetMetadata(topic, TimeSpan.FromSeconds(10));
                        if (metadata.Topics.Any(x => x.Topic == topic))
                        {
                            await client.DeleteTopicsAsync(new string[] { topic });
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{nameof(KafkaCommon)} failed to delete topic {topic}", ex);
                    }
                }
            }
            finally
            {
                _ = locker.Release();
            }
        }

        //public static async Task DeleteAllAckTopics(string host, string topic)
        //{
        //    var clientConfig = new AdminClientConfig();
        //    clientConfig.BootstrapServers = host;

        //    using (var client = new AdminClientBuilder(clientConfig).Build())
        //    {
        //        try
        //        {
        //            var metadata = client.GetMetadata(TimeSpan.FromSeconds(10));
        //            foreach (var item in metadata.Topics)
        //            {
        //                if (item.Topic.StartsWith("ACK-"))
        //                {
        //                    try
        //                    {
        //                        await client.DeleteTopicsAsync(new string[] { topic });
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        throw new Exception($"{nameof(KafkaCommon)} failed to delete topic {topic}", ex);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new Exception($"{nameof(KafkaCommon)} failed to delete topics {topic}", ex);
        //        }
        //    }
        //}
    }
}
