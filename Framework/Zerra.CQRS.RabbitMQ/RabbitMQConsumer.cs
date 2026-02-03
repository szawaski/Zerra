// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using RabbitMQ.Client;
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
        private readonly ILogger? log;
        private readonly string? environment;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private readonly HashSet<Type> commandTypes;
        private readonly HashSet<Type> eventTypes;

        private IConnection? connection = null;
        private HandleRemoteCommandDispatch? commandHandlerAsync = null;
        private HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync = null;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private CommandCounter? commandCounter = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQConsumer"/> class.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address.</param>
        /// <param name="serializer">The serializer for message deserialization and serialization.</param>
        /// <param name="encryptor">Optional decryptor for message decryption. If null, messages are assumed to be unencrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to match exchange name prefixes for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public RabbitMQConsumer(string host, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
            this.environment = environment;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();
        }

        string ICommandConsumer.MessageHost => "[Host has Secrets]";
        string IEventConsumer.MessageHost => "[Host has Secrets]";

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (this.connection is not null)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
            this.commandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }
        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            if (this.connection is not null)
                throw new InvalidOperationException("Connection already open");
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
            if (this.connection is not null)
                return;

            try
            {
                var factory = new ConnectionFactory() { HostName = host, DispatchConsumersAsync = true };
                this.connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                log?.Error($"{nameof(RabbitMQConsumer)} failed to open", ex);
                throw;
            }

            lock (commandExchanges)
            {
                lock (eventExchanges)
                {
                    OpenExchanges();
                }
            }
        }

        private void OpenExchanges()
        {
            if (this.connection is null)
                return;

            foreach (var exchange in commandExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.connection);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.connection);
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
            foreach (var exchange in commandExchanges.Values.Where(x => x.IsOpen))
                exchange.Dispose();
            foreach (var exchange in eventExchanges.Values.Where(x => x.IsOpen))
                exchange.Dispose();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();

            if (this.connection is not null)
            {
                this.connection.Close();
                this.connection.Dispose();
                this.connection = null;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQConsumer"/>.
        /// </summary>
        /// <remarks>
        /// Closes all open message exchanges and the RabbitMQ connection.
        /// After disposal, the consumer cannot be used.
        /// </remarks>
        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (commandCounter is null || commandHandlerAsync is null || commandHandlerAwaitAsync is null || commandHandlerWithResultAwaitAsync is null)
                throw new Exception($"{nameof(RabbitMQConsumer)} is not setup");

            lock (commandExchanges)
            {
                if (commandTypes.Contains(type))
                    return;
                if (commandExchanges.ContainsKey(topic))
                    return;
                _ = commandTypes.Add(type);
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, commandCounter, topic, serializer, encryptor, log, environment, commandHandlerAsync, commandHandlerAwaitAsync, commandHandlerWithResultAwaitAsync));
                OpenExchanges();
            }
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (eventHandlerAsync is null)
                throw new Exception($"{nameof(RabbitMQConsumer)} is not setup");

            lock (eventExchanges)
            {
                if (eventTypes.Contains(type))
                    return;
                if (eventExchanges.ContainsKey(topic))
                    return;
                _ = eventTypes.Add(type);
                eventExchanges.Add(topic, new EventConsumer(maxConcurrent, topic, serializer, encryptor, log, environment, eventHandlerAsync));
                OpenExchanges();
            }
        }
    }
}
