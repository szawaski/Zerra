// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMQ
{
    public partial class RabbitMQServer : ICommandServer, IEventServer, IDisposable
    {
        private const int retryDelay = 10000;
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.RijndaelManaged;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        private readonly List<CommandReceiverExchange> commandExchanges;
        private readonly List<EventReceiverExchange> eventExchanges;

        private IConnection connection = null;
        private Func<ICommand, Task> commandHandlerAsync = null;
        private Func<ICommand, Task> commandHandlerAwaitAsync = null;
        private Func<IEvent, Task> eventHandlerAsync = null;

        public string ConnectionString => host;

        public RabbitMQServer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;
            this.commandExchanges = new List<CommandReceiverExchange>();
            this.eventExchanges = new List<EventReceiverExchange>();
        }

        void ICommandServer.SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
        {
            if (this.connection != null)
                throw new InvalidOperationException("Connection already open");
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventServer.SetHandler(Func<IEvent, Task> handlerAsync)
        {
            if (this.connection != null)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandServer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(RabbitMQServer)} Command Server Started Connected To {this.host}");
        }
        void IEventServer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(RabbitMQServer)} Event Server Started Connected To {this.host}");
        }
        private void Open()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = host, DispatchConsumersAsync = true };
                this.connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                Log.ErrorAsync($"{nameof(RabbitMQServer)} failed to open", ex);
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

            foreach (var exchange in commandExchanges.Where(x => !x.IsOpen))
                exchange.Open(this.connection, this.commandHandlerAsync, this.commandHandlerAwaitAsync);

            foreach (var exchange in eventExchanges.Where(x => !x.IsOpen))
                exchange.Open(this.connection, this.eventHandlerAsync);
        }

        void ICommandServer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(RabbitMQServer)} Command Server Closed On {this.host}");
        }
        void IEventServer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(RabbitMQServer)} Event Server Closed On {this.host}");
        }
        private void Close()
        {
            if (this.connection != null)
            {
                foreach (var exchange in commandExchanges.Where(x => x.IsOpen))
                    exchange.Dispose();
                foreach (var exchange in eventExchanges.Where(x => x.IsOpen))
                    exchange.Dispose();
                this.commandExchanges.Clear();
                this.eventExchanges.Clear();
                this.connection.Close();
                this.connection.Dispose();
                this.connection = null;
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        void ICommandServer.RegisterCommandType(Type type)
        {
            lock (commandExchanges)
            {
                commandExchanges.Add(new CommandReceiverExchange(type, encryptionKey));
                OpenExchanges();
            }
        }
        ICollection<Type> ICommandServer.GetCommandTypes()
        {
            return commandExchanges.Select(x => x.Type).ToArray();
        }

        void IEventServer.RegisterEventType(Type type)
        {
            lock (eventExchanges)
            {
                eventExchanges.Add(new EventReceiverExchange(type, encryptionKey));
                OpenExchanges();
            }
        }
        ICollection<Type> IEventServer.GetEventTypes()
        {
            return eventExchanges.Select(x => x.Type).ToArray();
        }
    }
}
