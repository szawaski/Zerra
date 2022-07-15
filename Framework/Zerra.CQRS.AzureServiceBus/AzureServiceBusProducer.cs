// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public class AzureServiceBusProducer : ICommandProducer, IEventProducer, IAsyncDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private bool listenerStarted = false;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        private readonly string ackTopic;
        private readonly ServiceBusClient client;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public AzureServiceBusProducer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;

            var clientID = Guid.NewGuid().ToString();
            this.ackTopic = $"ACK-{clientID}";

            client = new ServiceBusClient(host);

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

            var topic = command.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureServiceBusCommandMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = AzureServiceBusCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            if (requireAcknowledgement)
            {
                var ackKey = Guid.NewGuid().ToString();

                var waiter = new SemaphoreSlim(0, 1);

                try
                {

                    Acknowledgement ack = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        ack = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var serviceBusMessage = new ServiceBusMessage(body);
                    serviceBusMessage.ReplyTo = ackTopic;
                    serviceBusMessage.ReplyToSessionId = ackKey;
                    await using (var sender = client.CreateSender(topic))
                    {
                        await sender.SendMessageAsync(serviceBusMessage);
                    }

                    await waiter.WaitAsync();
                    if (!ack.Success)
                        throw new AcknowledgementException(ack, topic);
                }
                finally
                {
                    if (waiter != null)
                        waiter.Dispose();
                }
            }
            else
            {
                var serviceBusMessage = new ServiceBusMessage(body);
                await using (var sender = client.CreateSender(topic))
                {
                    await sender.SendMessageAsync(serviceBusMessage);
                }
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            var topic = @event.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureServiceBusEventMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = AzureServiceBusCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            var serviceBusMessage = new ServiceBusMessage(body);
            await using (var sender = client.CreateSender(topic))
            {
                await sender.SendMessageAsync(serviceBusMessage);
            }
        }

        private async Task AckListeningThread()
        {

        retry:

            try
            {
                await AzureServiceBusCommon.EnsureTopic(host, ackTopic);

                await using (var receiver = client.CreateReceiver(ackTopic, ackTopic))
                {
                    for (; ; )
                    {
                        try
                        {
                            var serviceBusMessage = await receiver.ReceiveMessageAsync();
                            await receiver.CompleteMessageAsync(serviceBusMessage);

                            if (!ackCallbacks.TryRemove(serviceBusMessage.SessionId, out var callback))
                                continue;

                            var response = serviceBusMessage.Body.ToStream();
                            if (encryptionKey != null)
                                response = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, response, false);
                            var ack = await AzureServiceBusCommon.DeserializeAsync<Acknowledgement>(response);

                            callback(ack);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch { }
                    }
                }
                await AzureServiceBusCommon.DeleteTopic(host, ackTopic);
            }
            catch (Exception ex)
            {
                if (!canceller.IsCancellationRequested)
                {
                    _ = Log.ErrorAsync(ex);
                    await Task.Delay(AzureServiceBusCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                canceller.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            canceller.Cancel();
            await client.DisposeAsync();
        }
    }
}
