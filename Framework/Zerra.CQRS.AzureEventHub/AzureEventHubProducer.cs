// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricKey encryptionKey;
        public AzureEventHubProducer(string host, string eventHubName, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.eventHubName = eventHubName;
            this.encryptionKey = encryptionKey;
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventProducer.DispatchAsync(IEvent @event) { return SendAsync(@event); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            var partition = command.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventHubCommandMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = AzureEventHubCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            await using (var producer = new EventHubProducerClient(host, eventHubName))
            {
                string firstPartition = (await producer.GetPartitionIdsAsync()).First();

                var batchOptions = new CreateBatchOptions
                {
                    PartitionId = partition
                };

                using (var eventBatch = await producer.CreateBatchAsync(batchOptions))
                {
                    var eventsToSend = new List<EventData>();

                    for (var index = 0; index < 10; ++index)
                    {
                        var eventData = new EventData(body);
                        eventsToSend.Add(eventData);
                    }

                    await producer.SendAsync(eventsToSend);
                }

                await producer.CloseAsync();
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            var partition = @event.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventHubEventMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = AzureEventHubCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            await using (var producer = new EventHubProducerClient(host, eventHubName))
            {
                string firstPartition = (await producer.GetPartitionIdsAsync()).First();

                var batchOptions = new CreateBatchOptions
                {
                    PartitionId = partition
                };

                using (var eventBatch = await producer.CreateBatchAsync(batchOptions))
                {
                    var eventsToSend = new List<EventData>();

                    for (var index = 0; index < 10; ++index)
                    {
                        var eventData = new EventData(body);
                        eventsToSend.Add(eventData);
                    }

                    await producer.SendAsync(eventsToSend);
                }

                await producer.CloseAsync();
            }
        }

        private async Task AckListeningThread()
        {

        }

        public void Dispose()
        {
      
        }
    }
}
