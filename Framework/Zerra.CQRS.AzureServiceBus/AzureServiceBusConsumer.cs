// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    /// <summary>
    /// Azure Service Bus implementation of command and event consumer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Provides high-performance, reliable message consumption from Azure Service Bus queues and topics.
    /// Manages multiple exchanges (queues/topics) with concurrent processing capabilities.
    /// Thread-safe for concurrent operations.
    /// </remarks>
    public sealed partial class AzureServiceBusConsumer : ICommandConsumer, IEventConsumer, IDisposable, IAsyncDisposable
    {
        private readonly AzureServiceBusCommonNamespace commonNamespace;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ILogger? log;
        private readonly string? environment;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private readonly ConcurrentDictionary<string, Type> commandTypes;
        private readonly ConcurrentDictionary<string, Type> eventTypes;
        private readonly ServiceBusClient client;

        private bool isOpen;
        private HandleRemoteCommandDispatch? commandHandlerAsync = null;
        private HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private CommandCounter? commandCounter = null;
        private string? serviceName = null;

        private static readonly ServiceBusReceiverOptions receiverOptions = new()
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusConsumer"/> class.
        /// </summary>
        /// <param name="host">The Azure Service Bus connection string.</param>
        /// <param name="serializer">The serializer for message deserialization and serialization.</param>
        /// <param name="encryptor">Optional decryptor for message decryption. If null, messages are assumed to be unencrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information.</param>
        /// <param name="environment">Optional environment name to match queue and topic name prefixes for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public AzureServiceBusConsumer(string host, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.commonNamespace = AzureServiceBusCommon.GetNamespace(host);
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.log = log;
            this.environment = environment;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();

            this.client = new ServiceBusClient(host);
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
            log?.Info($"{nameof(AzureServiceBusConsumer)} Command Consumer Listening");
        }
        void IEventConsumer.Open()
        {
            Open();
            log?.Info($"{nameof(AzureServiceBusConsumer)} Event Consumer Listening");
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
                exchange.Open(this.commonNamespace, this.client);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.commonNamespace, this.client);
        }

        void ICommandConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(AzureServiceBusConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            log?.Info($"{nameof(AzureServiceBusConsumer)} Event Consumer Closed");
        }
        private void Close()
        {
            if (isOpen)
            {
                //stops the listeners, the messages already received keep going until they finish
                foreach (var exchange in commandExchanges.Values.Where(x => x.IsOpen))
                    exchange.Close();
                foreach (var exchange in eventExchanges.Values.Where(x => x.IsOpen))
                    exchange.Close();
                isOpen = false;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            this.Close();
            //each exchange waits for the messages it is still handling
            foreach (var exchange in commandExchanges.Values)
                await exchange.DisposeAsync();
            foreach (var exchange in eventExchanges.Values)
                await exchange.DisposeAsync();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();
            await client.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Close();
            //each exchange waits for the messages it is still handling
            foreach (var exchange in commandExchanges.Values)
                exchange.Dispose();
            foreach (var exchange in eventExchanges.Values)
                exchange.Dispose();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();
            _ = client.DisposeAsync().AsTask();
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (commandHandlerAsync is null || commandHandlerAwaitAsync is null || commandHandlerWithResultAwaitAsync is null)
                throw new Exception($"{nameof(AzureServiceBusConsumer)} is not setup");

            lock (commandExchanges)
            {
                var existing = commandTypes.GetOrAdd(type.Name, type);
                if (existing != type)
                    throw new InvalidOperationException($"{type.FullName} has the same name as {existing.FullName}, the name is what's sent between services");
                if (commandExchanges.ContainsKey(topic))
                    return;
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, commandCounter, topic, commandTypes, serializer, encryptor, compressor, log, environment, commandHandlerAsync, commandHandlerAwaitAsync, commandHandlerWithResultAwaitAsync));
                OpenExchanges();
            }
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type, EventConsumerMode eventConsumerMode)
        {
            if (eventHandlerAsync is null || serviceName is null)
                throw new Exception($"{nameof(AzureServiceBusConsumer)} is not setup");

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
