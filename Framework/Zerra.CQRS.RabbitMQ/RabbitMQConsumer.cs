// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using RabbitMQ.Client;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    /// <summary>
    /// RabbitMQ implementation of command and event consumer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Manages multiple RabbitMQ exchanges for consuming commands and events with configurable concurrency.
    /// Provides automatic connection management, exchange creation, and optional message decryption.
    /// Thread-safe for concurrent operations.
    /// </remarks>
    public sealed partial class RabbitMQConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private readonly string host;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly bool resilientCommands;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private readonly ConcurrentDictionary<string, Type> commandTypes;
        private readonly ConcurrentDictionary<string, Type> eventTypes;

        private readonly Func<Task<IConnection>> getConnection;
        private readonly SemaphoreSlim connectionLock;
        private IConnection? connection = null;
        private bool isOpen = false;
        private volatile bool disposed = false;
        private HandleRemoteCommandDispatch? commandHandlerAsync = null;
        private HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync = null;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private CommandCounter? commandCounter = null;
        private string? serviceName = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQConsumer"/> class.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address, or an AMQP URI (amqp://user:password@host:port/vhost, amqps:// for TLS) to also configure credentials, port, virtual host, and TLS.</param>
        /// <param name="serializer">The serializer for message deserialization and serialization.</param>
        /// <param name="encryptor">Optional decryptor for message decryption. If null, messages are assumed to be unencrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to match exchange name prefixes for isolation.</param>
        /// <param name="resilientCommands">True to acknowledge a command once its handler finishes instead of when it's received, so a command being handled when the process stops is delivered again. A command can then run more than once.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public RabbitMQConsumer(string host, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment, bool resilientCommands = false)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.log = log;
            this.environment = environment;
            this.resilientCommands = resilientCommands;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();
            this.getConnection = GetConnectionAsync;
            this.connectionLock = new SemaphoreSlim(1, 1);
        }

        string ICommandConsumer.MessageHost => "[Host has Secrets]";
        string IEventConsumer.MessageHost => "[Host has Secrets]";

        void ICommandConsumer.Setup(CommandCounter? commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (commandHandlerAsync is not null)
                throw new InvalidOperationException("Command consumer already setup");
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
            this.commandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }
        void IEventConsumer.Setup(string serviceName, HandleRemoteEventDispatch handlerAsync)
        {
            if (eventHandlerAsync is not null)
                throw new InvalidOperationException("Event consumer already setup");
            this.serviceName = serviceName;
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            log?.Info($"{nameof(RabbitMQConsumer)} Command Consumer Listening");
        }
        void IEventConsumer.Open()
        {
            Open();
            log?.Info($"{nameof(RabbitMQConsumer)} Event Consumer Listening");
        }
        private void Open()
        {
            isOpen = true;

            lock (commandExchanges)
            {
                lock (eventExchanges)
                {
                    OpenExchanges();
                }
            }
        }

        //the exchanges connect when they start listening, so one that can't reach the broker logs it and retries
        private async Task<IConnection> GetConnectionAsync()
        {
            var connection = this.connection;
            if (connection is not null)
                return connection;

            await connectionLock.WaitAsync();
            try
            {
                if (this.connection is not null)
                    return this.connection;
                if (disposed)
                    throw new ObjectDisposedException(nameof(RabbitMQConsumer));

                var factory = RabbitMQCommon.CreateConnectionFactory(host);
                connection = await factory.CreateConnectionAsync();
                _ = Interlocked.Exchange(ref this.connection, connection);
                //disposed while connecting, Dispose may have missed this connection
                if (disposed)
                {
                    Interlocked.Exchange(ref this.connection, null)?.Dispose();
                    throw new ObjectDisposedException(nameof(RabbitMQConsumer));
                }
                return connection;
            }
            finally
            {
                _ = connectionLock.Release();
            }
        }

        private void OpenExchanges()
        {
            if (!isOpen)
                return;

            foreach (var exchange in commandExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(getConnection);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(getConnection);
        }

        void ICommandConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(RabbitMQConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(RabbitMQConsumer)} Event Consumer Closed");
        }
        private void Close()
        {
            //stops the deliveries, the connection stays open so the messages already received can still be acknowledged and replied to
            foreach (var exchange in commandExchanges.Values.Where(x => x.IsOpen))
                exchange.Close();
            foreach (var exchange in eventExchanges.Values.Where(x => x.IsOpen))
                exchange.Close();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQConsumer"/>.
        /// </summary>
        /// <remarks>
        /// Closes all open message exchanges, waits for the messages they are still handling to finish, and closes the RabbitMQ connection.
        /// After disposal, the consumer cannot be used.
        /// </remarks>
        public void Dispose()
        {
            this.Close();
            //each exchange waits for the messages it is still handling, then closing its channel returns the unacknowledged ones to their queue
            foreach (var exchange in commandExchanges.Values)
                exchange.Dispose();
            foreach (var exchange in eventExchanges.Values)
                exchange.Dispose();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();

            disposed = true;
            //disposing closes the connection, ignoring one the broker or network already closed
            Interlocked.Exchange(ref this.connection, null)?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQConsumer"/>.
        /// </summary>
        /// <remarks>
        /// Closes all open message exchanges, waits for the messages they are still handling to finish, and closes the RabbitMQ connection.
        /// After disposal, the consumer cannot be used.
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            this.Close();
            //each exchange waits for the messages it is still handling, then closing its channel returns the unacknowledged ones to their queue
            foreach (var exchange in commandExchanges.Values)
                await exchange.DisposeAsync();
            foreach (var exchange in eventExchanges.Values)
                await exchange.DisposeAsync();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();

            disposed = true;
            //disposing closes the connection, ignoring one the broker or network already closed
            var connection = Interlocked.Exchange(ref this.connection, null);
            if (connection is not null)
                await connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (commandHandlerAsync is null || commandHandlerAwaitAsync is null || commandHandlerWithResultAwaitAsync is null)
                throw new Exception($"{nameof(RabbitMQConsumer)} is not setup");

            lock (commandExchanges)
            {
                var existing = commandTypes.GetOrAdd(type.Name, type);
                if (existing != type)
                    throw new InvalidOperationException($"{type.FullName} has the same name as {existing.FullName}, the name is what's sent between services");
                if (commandExchanges.ContainsKey(topic))
                    return;
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, commandCounter, topic, commandTypes, serializer, encryptor, compressor, log, environment, commandHandlerAsync, commandHandlerAwaitAsync, commandHandlerWithResultAwaitAsync, resilientCommands));
                OpenExchanges();
            }
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type, EventConsumerMode eventConsumerMode)
        {
            if (eventHandlerAsync is null || serviceName is null)
                throw new Exception($"{nameof(RabbitMQConsumer)} is not setup");

            lock (eventExchanges)
            {
                var existing = eventTypes.GetOrAdd(type.Name, type);
                if (existing != type)
                    throw new InvalidOperationException($"{type.FullName} has the same name as {existing.FullName}, the name is what's sent between services");
                if (eventExchanges.ContainsKey(topic))
                    return;
                eventExchanges.Add(topic, new EventConsumer(maxConcurrent, topic, eventTypes, serializer, encryptor, compressor, log, environment, serviceName, eventConsumerMode, eventHandlerAsync));
                OpenExchanges();
            }
        }
    }
}
