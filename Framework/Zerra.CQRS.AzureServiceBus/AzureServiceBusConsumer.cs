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
        private static readonly string applicationName = Config.EntryAssemblyName;

        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;

        private readonly List<CommandConsumer> commandExchanges;
        private readonly List<EventConsumer> eventExchanges;
        private readonly ServiceBusClient client;

        private bool isOpen;
        private HandleRemoteCommandDispatch commandHandlerAsync = null;
        private HandleRemoteCommandDispatch commandHandlerAwaitAsync = null;
        private HandleRemoteEventDispatch eventHandlerAsync = null;

        public string ServiceUrl => host;

        private static readonly ServiceBusReceiverOptions receiverOptions = new()
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
        };

        public AzureServiceBusConsumer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            this.commandExchanges = new List<CommandConsumer>();
            this.eventExchanges = new List<EventConsumer>();

            client = new ServiceBusClient(host);
        }

        void ICommandConsumer.SetHandler(HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.SetHandler(HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Command Consumer Started Connected");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureServiceBusConsumer)} Event Consumer Started Connected");
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

            foreach (var exchange in commandExchanges.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.client, this.commandHandlerAsync, this.commandHandlerAwaitAsync);

            foreach (var exchange in eventExchanges.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.client, this.eventHandlerAsync);
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
                foreach (var exchange in commandExchanges.Where(x => x.IsOpen))
                    exchange.Dispose();
                foreach (var exchange in eventExchanges.Where(x => x.IsOpen))
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

        void ICommandConsumer.RegisterCommandType(Type type)
        {
            lock (commandExchanges)
            {
                commandExchanges.Add(new CommandConsumer(type, symmetricConfig, environment));
                OpenExchanges();
            }
        }
        IEnumerable<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandExchanges.Select(x => x.Type);
        }

        void IEventConsumer.RegisterEventType(Type type)
        {
            lock (eventExchanges)
            {
                eventExchanges.Add(new EventConsumer(type, symmetricConfig, environment));
                OpenExchanges();
            }
        }
        IEnumerable<Type> IEventConsumer.GetEventTypes()
        {
            return eventExchanges.Select(x => x.Type);
        }
    }
}
