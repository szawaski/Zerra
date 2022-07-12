// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureEventHub
{
    public partial class AzureEventHubConsumer
    {
        public class CommandConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string partition;
            private readonly SymmetricKey encryptionKey;

            private CancellationTokenSource canceller = null;

            public CommandConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.partition = type.GetNiceName();
                this.encryptionKey = encryptionKey;
            }

            public void Open(string host, string eventHubName, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, eventHubName, handlerAsync, handlerAwaitAsync));
            }

            private async Task ListeningThread(string host, string eventHubName, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {
                    string ackKey = null;
                    var awaitResponse = false;
                    Exception error = null;

                    await using (var consumer = new EventHubConsumerClient(partition, host, eventHubName))
                    {
                        await foreach (var partitionEvent in consumer.ReadEventsAsync(canceller.Token))
                        {
                            if (partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckKey, out var ackKeyObject))
                            {
                                if (ackKeyObject is string ackKeyString)
                                {
                                    ackKey = ackKeyString;
                                    awaitResponse = true;
                                }
                            }

                            var stopwatch = new Stopwatch();
                            stopwatch.Start();

                            var body = partitionEvent.Data.EventBody.ToArray();
                            if (encryptionKey != null)
                                body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, body);

                            var message = AzureEventHubCommon.Deserialize<AzureEventHubCommandMessage>(body);

                            if (message.Claims != null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            if (awaitResponse)
                                await handlerAwaitAsync(message.Message);
                            else
                                await handlerAsync(message.Message);

                            stopwatch.Stop();
                            _ = Log.TraceAsync($"Received Await: {partition}  {stopwatch.ElapsedMilliseconds}");
                        }
                    }

                    if (awaitResponse)
                    {
                        try
                        {
                            var ack = new Acknowledgement()
                            {
                                Success = error == null,
                                ErrorMessage = error?.Message
                            };
                            var body = AzureEventHubCommon.Serialize(ack);
                            if (encryptionKey != null)
                                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

                            await using (var producer = new EventHubProducerClient(host, eventHubName))
                            {
                                var batchOptions = new CreateBatchOptions
                                {
                                    PartitionId = partition,
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

                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);

                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(AzureEventHubCommon.RetryDelay);
                        goto retry;
                    }
                }
                canceller.Dispose();
                IsOpen = false;
            }

            public void Dispose()
            {
                if (canceller != null)
                    canceller.Cancel();
            }
        }
    }
}
