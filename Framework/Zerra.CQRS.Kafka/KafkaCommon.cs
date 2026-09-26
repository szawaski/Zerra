// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.Kafka
{
    internal static class KafkaCommon
    {
        public const int TopicMaxLength = 249;

        public const int RetryDelay = 5000;

        public const string MessageKey = "Body";
        public const string MessageWithAckKey = "BodyAck";
        public const string AckTopicHeader = "AckTopic";
        public const string AckKeyHeader = "AckKey";

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj);
        }

        public static object? Deserialize(byte[] data, Type type)
        {
            return ByteSerializer.Deserialize(data, type);
        }

        public static T? Deserialize<T>(byte[] bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes);
        }

        private static readonly ConcurrentDictionary<(string Host, string? UserName), KafkaCommonHost> hosts = new();

        public static KafkaCommonHost GetHost(string host, string? userName, string? password)
        {
            var key = (host, userName);
            if (hosts.TryGetValue(key, out var existing))
                return existing;
            var created = new KafkaCommonHost(host, userName, password);
            existing = hosts.GetOrAdd(key, created);
            if (existing != created)
                created.Client.Dispose();
            return existing;
        }

        //returns before its first await when the topic is known, which completes without a Task
        public static async ValueTask EnsureTopic(KafkaCommonHost kafkaHost, string topic)
        {
            var topics = kafkaHost.Topics;
            if (topics is not null && topics.Contains(topic))
                return;

            await kafkaHost.Locker.WaitAsync();
            try
            {
                if (kafkaHost.Topics is not null && kafkaHost.Topics.Contains(topic))
                    return;

                try
                {
                    if (kafkaHost.Topics is null)
                    {
                        //listing every topic names none, so it can't have the broker auto create one
                        var metadata = kafkaHost.Client.GetMetadata(TimeSpan.FromSeconds(10));
                        var listed = new HashSet<string>(metadata.Topics.Where(x => x.Error.Code == ErrorCode.NoError).Select(x => x.Topic));
                        kafkaHost.Topics = listed;
                        if (listed.Contains(topic))
                            return;
                    }

                    var topicSpecification = new TopicSpecification()
                    {
                        Name = topic,
                        ReplicationFactor = 1,
                        NumPartitions = 1
                    };
                    try
                    {
                        await kafkaHost.Client.CreateTopicsAsync(new TopicSpecification[] { topicSpecification });
                    }
                    catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
                    {
                        //another service created it first
                    }
                    kafkaHost.Topics = new HashSet<string>(kafkaHost.Topics!) { topic };
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(KafkaCommon)} failed to create topic {topic}", ex);
                }
            }
            finally
            {
                _ = kafkaHost.Locker.Release();
            }
        }

        //for a uniquely named topic that can't exist yet, so the topics aren't listed
        public static async Task CreateTopic(KafkaCommonHost kafkaHost, string topic)
        {
            var topicSpecification = new TopicSpecification()
            {
                Name = topic,
                ReplicationFactor = 1,
                NumPartitions = 1
            };
            try
            {
                await kafkaHost.Client.CreateTopicsAsync(new TopicSpecification[] { topicSpecification });
            }
            catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
            {
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(KafkaCommon)} failed to create topic {topic}", ex);
            }
        }

        //a topic deleted outside of this process is still in the list, a consumer that fails forgets it so the next EnsureTopic checks the broker again
        public static async Task ForgetTopic(KafkaCommonHost kafkaHost, string topic)
        {
            await kafkaHost.Locker.WaitAsync();
            try
            {
                var topics = kafkaHost.Topics;
                if (topics is not null && topics.Contains(topic))
                {
                    var remaining = new HashSet<string>(topics);
                    _ = remaining.Remove(topic);
                    kafkaHost.Topics = remaining;
                }
            }
            finally
            {
                _ = kafkaHost.Locker.Release();
            }
        }

        public static Task DeleteTopic(string host, string? userName, string? password, string topic) => DeleteTopic(GetHost(host, userName, password), topic);

        public static async Task DeleteTopic(KafkaCommonHost kafkaHost, string topic)
        {
            await kafkaHost.Locker.WaitAsync();
            try
            {
                //deleted directly rather than checked first, a metadata request for a missing topic can create it again when the broker auto creates topics
                try
                {
                    await kafkaHost.Client.DeleteTopicsAsync(new string[] { topic });
                }
                catch (DeleteTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.UnknownTopicOrPart))
                {
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(KafkaCommon)} failed to delete topic {topic}", ex);
                }
                var topics = kafkaHost.Topics;
                if (topics is not null && topics.Contains(topic))
                {
                    var remaining = new HashSet<string>(topics);
                    _ = remaining.Remove(topic);
                    kafkaHost.Topics = remaining;
                }
            }
            finally
            {
                _ = kafkaHost.Locker.Release();
            }
        }

        //the group's consumers must have left first, a group that doesn't exist is already deleted
        public static Task DeleteConsumerGroup(string host, string? userName, string? password, string group) => DeleteConsumerGroup(GetHost(host, userName, password), group);

        public static async Task DeleteConsumerGroup(KafkaCommonHost kafkaHost, string group)
        {
            await kafkaHost.Locker.WaitAsync();
            try
            {
                try
                {
                    await kafkaHost.Client.DeleteGroupsAsync(new string[] { group });
                }
                catch (DeleteGroupsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.GroupIdNotFound))
                {
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(KafkaCommon)} failed to delete consumer group {group}", ex);
                }
            }
            finally
            {
                _ = kafkaHost.Locker.Release();
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
