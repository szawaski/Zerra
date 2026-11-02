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
        public async Task CancelledCallsStop()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            var stream = $"Cancel-{Guid.NewGuid():N}";
            var data = Encoding.UTF8.GetBytes("{}");

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var token = cancelled.Token;
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.AppendAsync(Guid.NewGuid(), "E", stream, null, EventStoreState.Any, data, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.TerminateAsync(Guid.NewGuid(), "E", stream, null, EventStoreState.Any, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.ReadAsync(stream, null, null, null, null, null, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.ReadBackwardsAsync(stream, null, null, null, null, null, token));
            Assert.Empty(await engine.ReadAsync(stream, null, null, null, null, null, TestContext.Current.CancellationToken));

            //one already on its way stops too
            _ = await engine.AppendAsync(Guid.NewGuid(), "E", stream, null, EventStoreState.Any, data, TestContext.Current.CancellationToken);
            using var running = new CancellationTokenSource();
            var read = engine.ReadAsync(stream, null, null, null, null, null, running.Token);
            running.Cancel();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        }

        [Fact]
        public async Task CancelledQueriesAndChangesStop()
        {
            using var engine = new KurrentDBEngine(KurrentDBTestDataContext.ConnectionString, KurrentDBTestDataContext.Insecure);
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<CancelModel>(engine));
            var model = new CancelModel { ID = Guid.NewGuid(), Number = 1 };
            await repo.CreateAsync(model, TestContext.Current.CancellationToken);

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var token = cancelled.Token;
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<CancelModel>().Where(x => x.ID == model.ID).ToArrayAsync(token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.EventManyAsync<CancelModel>(TemporalOrder.Oldest, (DateTime?)null, null, null, null, x => x.ID == model.ID, cancellationToken: token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.UpdateAsync(new CancelModel { ID = model.ID, Number = 2 }, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.DeleteAsync(model, token));
            var kept = await repo.Query<CancelModel>().Where(x => x.ID == model.ID).SingleOrDefaultAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, kept!.Number);

            var aggregate = new TestAggregate(Guid.NewGuid(), engine);
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregate.Append(new TestAggregateCreated { Name = "n", Amount = 1 }, false, token));
            Assert.False(await aggregate.Rebuild(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Entity("KurrentCancelModel")]
        public sealed class CancelModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
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
