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
        private readonly SymmetricConfig? symmetricConfig;
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

        public KafkaConsumer(string host, SymmetricConfig? symmetricConfig, string? environment, string? userName, string? password)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
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
                exchange.Open(this.host, this.userName, this.password);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.userName, this.password);
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
            if (commandCounter is null || commandHandlerAsync is null || commandHandlerAwaitAsync is null || commandHandlerWithResultAwaitAsync is null)
                throw new Exception($"{nameof(KafkaConsumer)} is not setup");

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
                eventTypes.Add(type);
                eventExchanges.Add(topic, new EventConsumer(maxConcurrent, topic, symmetricConfig, environment, eventHandlerAsync));
                OpenExchanges();
            }
        }
    }
}
