// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public partial class AzureServiceBusConsumer
    {
        public class CommandConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly string clientID;
            private readonly SymmetricKey encryptionKey;

            private CancellationTokenSource canceller = null;

            public CommandConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.topic = type.GetNiceName();
                this.clientID = Guid.NewGuid().ToString();
                this.encryptionKey = encryptionKey;
            }

            public void Open(string host, ServiceBusClient client, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, client, handlerAsync, handlerAwaitAsync));
            }

            private async Task ListeningThread(string host, ServiceBusClient client, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {
                    await AzureServiceBusCommon.EnsureTopic(host, topic);

                    await using (var receiver = client.CreateReceiver(topic, topic))
                    {
                        for (; ; )
                        {
                            Exception error = null;
                            var awaitResponse = false;
                            string ackTopic = null;
                            string ackKey = null;
                            try
                            {
                                var serviceBusMessage = await receiver.ReceiveMessageAsync();
                                await receiver.CompleteMessageAsync(serviceBusMessage);
                                if (awaitResponse)
                                {
                                    ackTopic = serviceBusMessage.ReplyTo;
                                    ackKey = serviceBusMessage.ReplyToSessionId;
                                }

                                var stopwatch = Stopwatch.StartNew();

                                var body = serviceBusMessage.Body.ToStream();
                                if (encryptionKey != null)
                                    body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, body, false);

                                var message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusCommandMessage>(body);

                                if (message.Claims != null)
                                {
                                    var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                                }

                                if (awaitResponse)
                                    await handlerAwaitAsync(message.Message);
                                else
                                    await handlerAsync(message.Message);

                                _ = Log.TraceAsync($"Received Await: {topic} {stopwatch.ElapsedMilliseconds}");
                            }
                            catch (TaskCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                _ = Log.TraceAsync($"Error: Received Await: {topic}");
                                _ = Log.ErrorAsync(ex);
                                error = ex;
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
                                    var body = AzureServiceBusCommon.Serialize(ack);
                                    if (encryptionKey != null)
                                        body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

                                    var serviceBusMessage = new ServiceBusMessage(body);
                                    serviceBusMessage.SessionId = ackKey;
                                    await using (var sender = client.CreateSender(ackTopic))
                                    {
                                        await sender.SendMessageAsync(serviceBusMessage);
                                    }
                                }

                                catch (Exception ex)
                                {
                                    _ = Log.ErrorAsync(ex);
                                }
                            }
                        }
                    }
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
