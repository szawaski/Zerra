// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMQ
{
    /// <summary>
    /// Checks whether a RabbitMQ server can be reached, such as at startup to choose between RabbitMQ and a direct transport.
    /// </summary>
    public static class RabbitMQConnection
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Tests the connection by opening and closing one. This is synchronous because the RabbitMQ client only connects synchronously.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address, or an AMQP URI (amqp://user:password@host:port/vhost, amqps:// for TLS).</param>
        /// <param name="timeout">How long to wait for the connection, five seconds if not given.</param>
        /// <returns>True if a connection opened; otherwise false.</returns>
        public static bool Test(string host, TimeSpan? timeout = null)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            try
            {
                var factory = RabbitMQCommon.CreateConnectionFactory(host);
                factory.RequestedConnectionTimeout = timeout ?? defaultTimeout;
                factory.AutomaticRecoveryEnabled = false;
                using (var connection = factory.CreateConnection())
                {
                    var isOpen = connection.IsOpen;
                    connection.Close();
                    return isOpen;
                }
            }
            catch (Exception ex)
            {
                _ = Log.WarnAsync($"{nameof(RabbitMQConnection)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
