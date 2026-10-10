// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using RabbitMQ.Client;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMQ
{
    /// <summary>
    /// Checks whether a RabbitMQ server can be reached, such as at startup to choose between RabbitMQ and a direct transport.
    /// </summary>
    public static class RabbitMQConnectionTest
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Tests the connection by opening and closing one.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address, or an AMQP URI (amqp://user:password@host:port/vhost, amqps:// for TLS).</param>
        /// <param name="timeout">How long to wait for the connection, five seconds if not given.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if a connection opened; otherwise false.</returns>
        public static async Task<bool> TestAsync(string host, TimeSpan? timeout = null, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            try
            {
                var factory = RabbitMQCommon.CreateConnectionFactory(host);
                factory.RequestedConnectionTimeout = timeout ?? defaultTimeout;
                factory.AutomaticRecoveryEnabled = false;
                using var cancellationTokenSource = new CancellationTokenSource(timeout ?? defaultTimeout);
                await using var connection = await factory.CreateConnectionAsync(cancellationTokenSource.Token);
                var isOpen = connection.IsOpen;
                await connection.CloseAsync(cancellationTokenSource.Token);
                return isOpen;
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(RabbitMQConnectionTest)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
