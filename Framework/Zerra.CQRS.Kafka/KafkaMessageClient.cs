// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.Encryption;

namespace Zerra.CQRS.Kafka
{
    //Kafka Producer
    public class KafkaMessageClient : ICommandClient, IEventClient, IDisposable
    {
        public KafkaMessageClient(string host, SymmetricKey encryptionKey)
        {

        }

        public string ServiceUrl => throw new NotImplementedException();

        public Task DispatchAsync(ICommand command)
        {
            throw new NotImplementedException();
        }

        public Task DispatchAsync(IEvent @event)
        {
            throw new NotImplementedException();
        }

        public Task DispatchAsyncAwait(ICommand command)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
