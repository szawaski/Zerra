// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;
using Zerra.Repository.KurrentDB;

namespace Zerra.Repository.Test.KurrentDB
{
    public class KurrentDBEngineTests
    {
        [Fact]
        public async Task TestSequenceEventStore()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            await RepoTest.TestSequenceEventStoreAsync(engine);
        }

        [Fact]
        public async Task TestEventStoreEngine()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            await EventStoreEngineTest.TestAsync(engine);
        }

        [Fact]
        public void SynchronousCalls_NotSupported()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            _ = Assert.Throws<NotSupportedException>(() => engine.Append(Guid.NewGuid(), "E", "Stream", null, EventStoreState.Any, [0]));
            _ = Assert.Throws<NotSupportedException>(() => engine.Terminate(Guid.NewGuid(), "E", "Stream", null, EventStoreState.Any));
            _ = Assert.Throws<NotSupportedException>(() => engine.Read("Stream", null, null, null, null, null));
            _ = Assert.Throws<NotSupportedException>(() => engine.ReadBackwards("Stream", null, null, null, null, null));
        }

        [Fact]
        public async Task TestSequenceAggregate()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            await AggregateTest.TestSequenceAsync(engine);
        }

        [Fact]
        public async Task TestAggregateConcurrency()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            await AggregateTest.TestConcurrencyAsync(engine);
        }

        [Fact]
        public async Task ConnectionTest_HealthCheckAnswers_IsValid()
        {
            //stands in for the server's HTTP health endpoint, the client may open its own connections too so every connection is accepted
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var healthChecks = 0;
            using var stop = new CancellationTokenSource();
            var serverTask = Task.Run(async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    TcpClient connection;
                    try { connection = await listener.AcceptTcpClientAsync(stop.Token); }
                    catch (OperationCanceledException) { return; }

                    _ = Task.Run(async () =>
                    {
                        using (connection)
                        {
                            using var stream = connection.GetStream();
                            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                            var requestLine = await reader.ReadLineAsync();
                            if (requestLine != "GET /health/live HTTP/1.1")
                                return;
                            while (!String.IsNullOrEmpty(await reader.ReadLineAsync())) { }
                            _ = Interlocked.Increment(ref healthChecks);
                            await stream.WriteAsync("HTTP/1.1 204 No Content\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray());
                        }
                    });
                }
            }, TestContext.Current.CancellationToken);

            var isValid = await Task.Run(() => KurrentDBConnectionTest.Test($"http://127.0.0.1:{port}", true));
            stop.Cancel();
            await serverTask;

            Assert.True(isValid);
            Assert.Equal(1, healthChecks);
        }

        [Fact]
        public void ConnectionTest_NothingListening_IsNotValid()
        {
            //a port that was free a moment ago, nothing accepts on it
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            Assert.False(KurrentDBConnectionTest.Test($"http://127.0.0.1:{port}", true));
        }
    }
}
