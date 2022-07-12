// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureEventHub
{
    public partial class AzureEventHubConsumer
    {
        public class EventConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly string clientID;
            private readonly SymmetricKey encryptionKey;

            private CancellationTokenSource canceller = null;

            public EventConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.topic = type.GetNiceName();
                this.clientID = Guid.NewGuid().ToString();
                this.encryptionKey = encryptionKey;
            }

            public void Open(string host, string eventHubName, Func<IEvent, Task> handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, eventHubName, handlerAsync));
            }

            private async Task ListeningThread(string host, string eventHubName, Func<IEvent, Task> handlerAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {

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
