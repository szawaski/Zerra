// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureEventHub
{
    //Kafka Consumer
    public partial class AzureEventHubConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;

        private bool isOpen;
        private Func<ICommand, Task> commandHandlerAsync = null;
        private Func<ICommand, Task> commandHandlerAwaitAsync = null;
        private Func<IEvent, Task> eventHandlerAsync = null;

        public string ConnectionString => host;

        public AzureEventHubConsumer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;
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
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Server Started Connected To {this.host}");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Server Started Connected To {this.host}");
        }
        private void Open()
        {
            isOpen = true;

            //lock (commandExchanges)
            //{
            //    lock (eventExchanges)
            //    {
            //        OpenExchanges();
            //    }
            //}
        }

        private void OpenExchanges()
        {
            if (!isOpen)
                return;

            //foreach (var exchange in commandExchanges.Where(x => !x.IsOpen))
            //    exchange.Open(this.host, this.commandHandlerAsync, this.commandHandlerAwaitAsync);

            //foreach (var exchange in eventExchanges.Where(x => !x.IsOpen))
            //    exchange.Open(this.host, this.eventHandlerAsync);
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Server Closed On {this.host}");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Server Closed On {this.host}");
        }
        private void Close()
        {
            if (!isOpen)
            {
                //foreach (var exchange in commandExchanges.Where(x => x.IsOpen))
                //    exchange.Dispose();
                //foreach (var exchange in eventExchanges.Where(x => x.IsOpen))
                //    exchange.Dispose();
                //this.commandExchanges.Clear();
                //this.eventExchanges.Clear();
                isOpen = false;
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        void ICommandConsumer.RegisterCommandType(Type type)
        {
            //lock (commandExchanges)
            //{
            //    commandExchanges.Add(new CommandConsumer(type, encryptionKey));
            //    OpenExchanges();
            //}
        }
        ICollection<Type> ICommandConsumer.GetCommandTypes()
        {
            throw new NotImplementedException();
            //return commandExchanges.Select(x => x.Type).ToArray();
        }

        void IEventConsumer.RegisterEventType(Type type)
        {
            //lock (eventExchanges)
            //{
            //    eventExchanges.Add(new EventConsumer(type, encryptionKey));
            //    OpenExchanges();
            //}
        }
        ICollection<Type> IEventConsumer.GetEventTypes()
        {
            throw new NotImplementedException();
            //return eventExchanges.Select(x => x.Type).ToArray();
        }
    }
}
