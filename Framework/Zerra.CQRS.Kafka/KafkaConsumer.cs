// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    //Kafka Consumer
    public partial class KafkaConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private const int retryDelay = 10000;
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;

        private readonly List<CommandConsumer> commandExchanges;
        private readonly List<EventConsumer> eventExchanges;

        private bool isOpen;
        private Func<ICommand, Task> commandHandlerAsync = null;
        private Func<ICommand, Task> commandHandlerAwaitAsync = null;
        private Func<IEvent, Task> eventHandlerAsync = null;

        public string ConnectionString => host;

        public KafkaConsumer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;
            this.commandExchanges = new List<CommandConsumer>();
            this.eventExchanges = new List<EventConsumer>();
        }

        void ICommandConsumer.SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.SetHandler(Func<IEvent, Task> handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Command Server Started Connected To {this.host}");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Event Server Started Connected To {this.host}");
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
                exchange.Open(this.host, this.commandHandlerAsync, this.commandHandlerAwaitAsync);

            foreach (var exchange in eventExchanges.Where(x => !x.IsOpen))
                exchange.Open(this.host, this.eventHandlerAsync);
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Command Server Closed On {this.host}");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(KafkaConsumer)} Event Server Closed On {this.host}");
        }
        private void Close()
        {
            if (!isOpen)
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

        public void Dispose()
        {
            this.Close();
        }

        void ICommandConsumer.RegisterCommandType(Type type)
        {
            lock (commandExchanges)
            {
                commandExchanges.Add(new CommandConsumer(type, encryptionKey));
                OpenExchanges();
            }
        }
        ICollection<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandExchanges.Select(x => x.Type).ToArray();
        }

        void IEventConsumer.RegisterEventType(Type type)
        {
            lock (eventExchanges)
            {
                eventExchanges.Add(new EventConsumer(type, encryptionKey));
                OpenExchanges();
            }
        }
        ICollection<Type> IEventConsumer.GetEventTypes()
        {
            return eventExchanges.Select(x => x.Type).ToArray();
        }
    }
}
