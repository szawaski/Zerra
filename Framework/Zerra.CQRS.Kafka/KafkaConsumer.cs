// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private HashSet<Type> commandTypes;
        private HashSet<Type> eventTypes;

        private bool isOpen;
        private HandleRemoteCommandDispatch commandHandlerAsync = null;
        private HandleRemoteCommandDispatch commandHandlerAwaitAsync = null;
        private HandleRemoteEventDispatch eventHandlerAsync = null;

        public string ServiceUrl => host;

        private CommandCounter commandCounter = null;

        public KafkaConsumer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            this.commandExchanges = new();
            this.eventExchanges = new();
            this.commandTypes = new();
            this.eventTypes = new();
        }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.Setup(CommandCounter commandCounter, HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Command Consumer Listening");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Event Consumer Listening");
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
                exchange.Open(this.host);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.host);
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Event Consumer Closed");
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

        public void Dispose()
        {
            this.Close();
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            lock (commandExchanges)
            {
                if (commandTypes.Contains(type))
                    return;
                if (commandExchanges.ContainsKey(topic))
                    return;
                commandTypes.Add(type);
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, commandCounter, topic, symmetricConfig, environment, commandHandlerAsync, commandHandlerAwaitAsync));
                OpenExchanges();
            }
        }
        IEnumerable<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandTypes;
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
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
