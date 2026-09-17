// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System.Diagnostics.CodeAnalysis;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    /// <summary>
    /// Checks whether a Kafka cluster can be reached, such as at startup to choose between Kafka and a direct transport.
    /// </summary>
    public static class KafkaConnection
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Tests the connection by asking the cluster to describe itself.
        /// </summary>
        /// <param name="host">The Kafka bootstrap server address (e.g., "localhost:9092").</param>
        /// <param name="userName">Optional username for SASL authentication. Must be paired with password.</param>
        /// <param name="password">Optional password for SASL authentication. Must be paired with userName.</param>
        /// <param name="timeout">How long to wait for the cluster, five seconds if not given.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the cluster answered; otherwise false.</returns>
        //Confluent.Kafka binds its native library by finding these methods and fields through reflection, which native AOT would otherwise trim away
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields, "Confluent.Kafka.Impl.Librdkafka", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Alpine", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Centos8", "Confluent.Kafka")]
        public static async Task<bool> TestAsync(string host, string? userName, string? password, TimeSpan? timeout = null, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            var clientConfig = new AdminClientConfig();
            clientConfig.BootstrapServers = host;
            if (userName is not null && password is not null)
            {
                clientConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                clientConfig.SaslMechanism = SaslMechanism.Plain;
                clientConfig.SaslUsername = userName;
                clientConfig.SaslPassword = password;
            }

            try
            {
                //the client otherwise writes every failed connection attempt to the console while it retries
                using (var client = new AdminClientBuilder(clientConfig).SetLogHandler(static (_, _) => { }).SetErrorHandler(static (_, _) => { }).Build())
                {
                    var result = await client.DescribeClusterAsync(new DescribeClusterOptions() { RequestTimeout = timeout ?? defaultTimeout });
                    return result.Nodes.Count > 0;
                }
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(KafkaConnection)} could not connect to {host}: {ex.Message}");
                return false;
            }
        }
    }
}
