// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;

namespace Zerra.CQRS.Kafka
{
    /// <summary>
    /// One admin client per host and user for the process, and the topics known to exist there, listed once and then kept by <see cref="KafkaCommon"/>.
    /// Each host has its own lock, so calls to one cluster don't wait on another. Producers and consumers get theirs once from <see cref="KafkaCommon.GetHost"/>.
    /// </summary>
    internal sealed class KafkaCommonHost
    {
        public readonly string Host;
        public readonly string? UserName;
        public readonly string? Password;
        public readonly bool UseTls;
        public readonly SemaphoreSlim Locker = new(1, 1);
        public readonly IAdminClient Client;
        //replaced rather than changed, under the lock, so it can be read without the lock
        public volatile HashSet<string>? Topics;

        public KafkaCommonHost(string host, string? userName, string? password, bool useTls)
        {
            Host = host;
            UserName = userName;
            Password = password;
            UseTls = useTls;

            var clientConfig = new AdminClientConfig();
            clientConfig.BootstrapServers = host;
            if (userName is not null && password is not null)
            {
                clientConfig.SecurityProtocol = useTls ? SecurityProtocol.SaslSsl : SecurityProtocol.SaslPlaintext;
                clientConfig.SaslMechanism = SaslMechanism.Plain;
                clientConfig.SaslUsername = userName;
                clientConfig.SaslPassword = password;
            }
            else if (useTls)
            {
                clientConfig.SecurityProtocol = SecurityProtocol.Ssl;
            }
            Client = new AdminClientBuilder(clientConfig).Build();
        }
    }
}
