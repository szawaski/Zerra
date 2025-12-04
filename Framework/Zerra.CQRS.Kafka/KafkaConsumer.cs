// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    /// <summary>
    /// Kafka implementation of command and event consumer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Manages multiple Kafka topics for consuming commands and events with configurable concurrency.
    /// Supports SASL authentication when username and password are provided.
    /// Provides automatic topic creation, connection management, and optional message decryption.
    /// Thread-safe for concurrent operations.
    /// </remarks>
    public sealed partial class KafkaConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private readonly string host;
        private readonly Zerra.Serialization.ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ILog? log;
        private readonly string? environment;
        private readonly string? userName;
        private readonly string? password;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private readonly HashSet<Type> commandTypes;
        private readonly HashSet<Type> eventTypes;

        private bool isOpen;
        private HandleRemoteCommandDispatch? commandHandlerAsync = null;
        private HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync = null;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private CommandCounter? commandCounter = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaConsumer"/> class.
        /// </summary>
        /// <param name="host">The Kafka bootstrap server address (e.g., "localhost:9092").</param>
        /// <param name="serializer">The serializer for message deserialization and serialization.</param>
        /// <param name="encryptor">Optional decryptor for message decryption. If null, messages are assumed to be unencrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to match topic name prefixes for isolation.</param>
        /// <param name="userName">Optional username for SASL authentication. Must be paired with password.</param>
        /// <param name="password">Optional password for SASL authentication. Must be paired with userName.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public KafkaConsumer(string host, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ILog? log, string? environment, string? userName, string? password)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
            this.environment = environment;
            this.userName = userName;
            this.password = password;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();
        }

        string ICommandConsumer.MessageHost => "[Host has Secrets]";
        string IEventConsumer.MessageHost => "[Host has Secrets]";

        /// <summary>
        /// Sets up the consumer with command handlers.
        /// </summary>
        /// <param name="commandCounter">The command counter for tracking sent commands.</param>
        /// <param name="handlerAsync">The asynchronous handler for processing commands.</param>
        /// <param name="handlerAwaitAsync">The awaitable asynchronous handler for processing commands.</param>
        /// <param name="handlerWithResultAwaitAsync">The asynchronous handler for processing commands with result.</param>
        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
            this.commandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }
        /// <summary>
        /// Sets up the consumer with event handlers.
        /// </summary>
        /// <param name="handlerAsync">The asynchronous handler for processing events.</param>
        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        /// <summary>
        /// Opens the consumer for receiving commands.
        /// </summary>
        void ICommandConsumer.Open()
        {
            Open();
            log?.Info($"{nameof(KafkaConsumer)} Command Consumer Listening");
        }
        /// <summary>
        /// Opens the consumer for receiving events.
        /// </summary>
        void IEventConsumer.Open()
        {
            Open();
            log?.Info($"{nameof(KafkaConsumer)} Event Consumer Listening");
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

        private void OpenExchanges()
        {
            if (!isOpen)
                return;

            foreach (var exchange in commandExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.userName, this.password);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.userName, this.password);
        }

        /// <summary>
        /// Closes the consumer, stopping command reception.
        /// </summary>
        void ICommandConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(KafkaConsumer)} Command Consumer Closed");
        }
        /// <summary>
        /// Closes the consumer, stopping event reception.
        /// </summary>
        void IEventConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(KafkaConsumer)} Event Consumer Closed");
        }
        private void Close()
        {
            if (isOpen)
            {
                foreach (var exchange in commandExchanges.Values.Where(x => x.IsOpen))
                    exchange.Dispose();
                foreach (var exchange in eventExchanges.Values.Where(x => x.IsOpen))
                    exchange.Dispose();
                this.commandExchanges.Clear();
                this.eventExchanges.Clear();
                isOpen = false;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KafkaConsumer"/>.
        /// </summary>
        /// <remarks>
        /// Closes all open message exchanges and cleans up associated resources.
        /// </remarks>
        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// Registers a command type with the consumer.
        /// </summary>
        /// <param name="maxConcurrent">The maximum number of concurrent messages to process for this command type.</param>
        /// <param name="topic">The Kafka topic name for the command.</param>
        /// <param name="type">The command type.</param>
        /// <exception cref="Exception">Thrown if the consumer is not properly setup.</exception>
        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (commandCounter is null || commandHandlerAsync is null || commandHandlerAwaitAsync is null || commandHandlerWithResultAwaitAsync is null)
                throw new Exception($"{nameof(KafkaConsumer)} is not setup");

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

        /// <summary>
        /// Registers an event type with the consumer.
        /// </summary>
        /// <param name="maxConcurrent">The maximum number of concurrent messages to process for this event type.</param>
        /// <param name="topic">The Kafka topic name for the event.</param>
        /// <param name="type">The event type.</param>
        /// <exception cref="Exception">Thrown if the consumer is not properly setup.</exception>
        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (eventHandlerAsync is null)
                throw new Exception($"{nameof(KafkaConsumer)} is not setup");

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
