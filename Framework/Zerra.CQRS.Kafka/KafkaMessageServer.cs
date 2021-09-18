// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.Encryption;

namespace Zerra.CQRS.Kafka
{
    //Kafka Consumer
    public class KafkaMessageServer : ICommandServer, IEventServer, IDisposable
    {
        public KafkaMessageServer(string host, SymmetricKey encryptionKey)
        {

        }

        public string ServiceUrl => throw new NotImplementedException();

        public ICollection<Type> GetCommandTypes()
        {
            throw new NotImplementedException();
        }

        public ICollection<Type> GetEventTypes()
        {
            throw new NotImplementedException();
        }

        public void RegisterCommandType(Type type)
        {
            throw new NotImplementedException();
        }

        public void RegisterEventType(Type type)
        {
            throw new NotImplementedException();
        }

        public void SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
        {
            throw new NotImplementedException();
        }

        public void SetHandler(Func<IEvent, Task> handlerAsync)
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
