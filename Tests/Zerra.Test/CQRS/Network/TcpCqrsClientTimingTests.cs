// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    [Collection(nameof(TimingSensitive))]
    public class TcpCqrsClientTimingTests
    {
        private static readonly ISerializer serializer = new ZerraByteSerializer();

        [Fact(Timeout = 10000)]
        public async Task CallTaskGeneric_CanceledDuringRequest_DoesNotWaitForAbort()
        {
            //the server accepts but never reads so a large request fills the socket buffers and the write waits
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();
            using var client = new TcpCqrsClient($"127.0.0.1:{((IPEndPoint)listener.LocalEndPoint!).Port}", serializer, null, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(TcpCqrsClientTests.ITestQueryHandler));
            using var cts = new CancellationTokenSource();

            var call = ((IQueryClient)client).CallTaskGeneric<int>(typeof(TcpCqrsClientTests.ITestQueryHandler), nameof(TcpCqrsClientTests.ITestQueryHandler.GetThings), [typeof(byte[])], [new byte[64 * 1024 * 1024]], "test-source", cts.Token);
            using var server = await listener.AcceptAsync(TestContext.Current.CancellationToken);
            await Task.Delay(200, TestContext.Current.CancellationToken); //the request fills the buffers and waits

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            cts.Cancel();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
            stopwatch.Stop();

            //the server doesn't have the whole request so there is no abort to acknowledge, waiting for one would add the abort timeout
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"cancel took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
