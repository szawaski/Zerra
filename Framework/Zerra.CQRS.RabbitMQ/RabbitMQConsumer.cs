// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed partial class RabbitMQConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {

        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;

        private readonly Dictionary<string, CommandConsumer> commandExchanges;
        private readonly Dictionary<string, EventConsumer> eventExchanges;
        private HashSet<Type> commandTypes;
        private HashSet<Type> eventTypes;

        private IConnection connection = null;
        private HandleRemoteCommandDispatch commandHandlerAsync = null;
        private HandleRemoteCommandDispatch commandHandlerAwaitAsync = null;
        private HandleRemoteEventDispatch eventHandlerAsync = null;

        public string ServiceUrl => host;

        private ReceiveCounter receiveCounter = null;

        public RabbitMQConsumer(string host, SymmetricConfig symmetricConfig, string environment)
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

        void ICommandConsumer.Setup(ReceiveCounter receiveCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            if (this.connection != null)
                throw new InvalidOperationException("Connection already open");
            this.receiveCounter = receiveCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.Setup(ReceiveCounter receiveCounter, HandleRemoteEventDispatch handlerAsync)
        {
            if (this.connection != null)
                throw new InvalidOperationException("Connection already open");
            this.receiveCounter = receiveCounter;
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(RabbitMQConsumer)} Command Consumer Started Connected");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(RabbitMQConsumer)} Event Consumer Started Connected");
        }
        private void Open()
        {
            if (this.connection != null)
                return;

            try
            {
                var factory = new ConnectionFactory() { HostName = host, DispatchConsumersAsync = true };
                this.connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"{nameof(RabbitMQConsumer)} failed to open", ex);
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
            if (this.connection == null)
                return;

            foreach (var exchange in commandExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.connection);

            foreach (var exchange in eventExchanges.Values.Where(x => !x.IsOpen))
                exchange.Open(this.connection);
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(RabbitMQConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(RabbitMQConsumer)} Event Consumer Closed");
        }
        private void Close()
        {
            foreach (var exchange in commandExchanges.Values.Where(x => x.IsOpen))
                exchange.Dispose();
            foreach (var exchange in eventExchanges.Values.Where(x => x.IsOpen))
                exchange.Dispose();
            this.commandExchanges.Clear();
            this.eventExchanges.Clear();

            if (this.connection != null)
            {
                this.connection.Close();
                this.connection.Dispose();
                this.connection = null;
            }
        }

        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
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
                commandExchanges.Add(topic, new CommandConsumer(maxConcurrent, receiveCounter, topic, symmetricConfig, environment, commandHandlerAsync, commandHandlerAwaitAsync));
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
                eventExchanges.Add(topic, new EventConsumer(maxConcurrent, receiveCounter, topic, symmetricConfig, environment, eventHandlerAsync));
                OpenExchanges();
            }
        }
        IEnumerable<Type> IEventConsumer.GetEventTypes()
        {
            return eventTypes;
        }
    }
}
