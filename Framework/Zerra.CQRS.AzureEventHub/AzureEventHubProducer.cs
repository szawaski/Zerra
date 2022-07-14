// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private bool listenerStarted = false;

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricKey encryptionKey;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public AzureEventHubProducer(string host, string eventHubName, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.eventHubName = eventHubName;
            this.encryptionKey = encryptionKey;

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventProducer.DispatchAsync(IEvent @event) { return SendAsync(@event); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            if (requireAcknowledgement)
            {
                lock (this)
                {
                    if (!listenerStarted)
                    {
                        _ = Task.Run(AckListeningThread);
                        listenerStarted = true;
                    }
                }
            }

            string ackKey = null;
            var type = command.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventHubMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = AzureEventHubCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            await using (var producer = new EventHubProducerClient(host, eventHubName))
            {
                if (requireAcknowledgement)
                    ackKey = Guid.NewGuid().ToString();

                var eventData = new EventData(body);
                eventData.Properties[AzureEventHubCommon.TypeProperty] = type;
                if (requireAcknowledgement)
                    eventData.Properties[AzureEventHubCommon.AckProperty] = ackKey;

                if (requireAcknowledgement)
                {
                    var waiter = new SemaphoreSlim(0, 1);

                    try
                    {
                        Acknowledgement ack = null;
                        _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                        {
                            ack = ackFromCallback;
                            _ = waiter.Release();
                        });

                        await producer.SendAsync(new EventData[] { eventData });

                        await waiter.WaitAsync();
                        if (!ack.Success)
                            throw new AcknowledgementException(ack, type);
                    }
                    finally
                    {
                        if (waiter != null)
                            waiter.Dispose();
                    }
                }
                else
                {
                    await producer.SendAsync(new EventData[] { eventData });
                }
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventHubMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = AzureEventHubCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            await using (var producer = new EventHubProducerClient(host, eventHubName))
            {
                using (var eventBatch = await producer.CreateBatchAsync())
                {
                    var eventData = new EventData(body);
                    await producer.SendAsync(new EventData[] { eventData });
                }

                await producer.CloseAsync();
            }
        }

        private async Task AckListeningThread()
        {
        retry:

            try
            {
                await using (var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, host, eventHubName))
                {
                    await foreach (var partitionEvent in consumer.ReadEventsAsync(canceller.Token))
                    {
                        if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckProperty, out var ackObject))
                            continue;
                        if (ackObject is not string ackString)
                            continue;
                        if (ackString != Boolean.TrueString)
                            continue;

                        string ackKey = null;
                        if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckKeyProperty, out var ackKeyObject))
                            continue;
                        if (ackKeyObject is not string ackKeyString)
                            continue;
                        ackKey = ackKeyString;

                        if (!ackCallbacks.TryRemove(ackKey, out var callback))
                            continue;

                        var response = partitionEvent.Data.EventBody.ToArray();
                        if (encryptionKey != null)
                            response = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, response);
                        var ack = AzureEventHubCommon.Deserialize<Acknowledgement>(response);

                        callback(ack);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!canceller.IsCancellationRequested)
                {
                    _ = Log.ErrorAsync(ex);
                    await Task.Delay(AzureEventHubCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                canceller.Dispose();
            }
        }

        public void Dispose()
        {
            canceller.Cancel();
        }
    }
}
