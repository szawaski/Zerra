// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer : ICommandConsumer, IEventConsumer, IAsyncDisposable
    {
        private readonly string host;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly string? environment;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private readonly HashSet<Type> commandTypes;
        private readonly HashSet<Type> eventTypes;
        private readonly ServiceBusClient client;

        private bool isOpen;
        private HandleRemoteCommandDispatch? commandHandlerAsync = null;
        private HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private CommandCounter? commandCounter = null;

        public string ServiceUrl => host;

        private static readonly ServiceBusReceiverOptions receiverOptions = new()
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
        };

        public AzureServiceBusConsumer(string host, SymmetricConfig? symmetricConfig, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();

            this.client = new ServiceBusClient(host);
        }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
            this.commandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }
        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Command Consumer Listening");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Event Consumer Listening");
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
                exchange.Open(this.host, this.client);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.client);
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Event Consumer Closed");
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

        public async ValueTask DisposeAsync()
        {
            this.Close();
            await client.DisposeAsync();
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (commandCounter == null || commandHandlerAsync == null || commandHandlerAwaitAsync == null || commandHandlerWithResultAwaitAsync == null)
                throw new Exception($"{nameof(AzureServiceBusConsumer)} is not setup");

            lock (commandExchanges)
            {
                if (commandTypes.Contains(type))
                    return;
                if (commandExchanges.ContainsKey(topic))
                    return;
                commandTypes.Add(type);
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, commandCounter, topic, symmetricConfig, environment, commandHandlerAsync, commandHandlerAwaitAsync, commandHandlerWithResultAwaitAsync));
                OpenExchanges();
            }
        }
        IEnumerable<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandTypes;
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (eventHandlerAsync == null)
                throw new Exception($"{nameof(AzureServiceBusConsumer)} is not setup");

            lock (eventExchanges)
            {
                if (eventTypes.Contains(type))
                    return;
                if (eventExchanges.ContainsKey(topic))
                    return;
                eventTypes.Add(type);
                eventExchanges.Add(topic, new EventConsumer(maxConcurrent, topic, symmetricConfig, environment, eventHandlerAsync));
                OpenExchanges();
            }
        }
        IEnumerable<Type> IEventConsumer.GetEventTypes()
        {
            return eventTypes;
        }
    }
}
