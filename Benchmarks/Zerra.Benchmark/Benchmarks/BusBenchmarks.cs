// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.Test.Helpers.Models;

namespace Zerra.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    [Config(typeof(PercentileConfig))]
    public class BusBenchmarks
    {
        private const int concurrent = 64;

        private sealed class PercentileConfig : ManualConfig
        {
            public PercentileConfig()
            {
                _ = AddColumn(StatisticColumn.Median, StatisticColumn.P95);
            }
        }

        public interface IBenchmarkQueryHandler : IQueryHandler
        {
            Task<NormalJsonModel> GetAsync(int id);
        }

        public sealed class BenchmarkCommand : ICommand
        {
            public int Value { get; set; }
        }

        public interface IBenchmarkCommandHandler : ICommandHandler<BenchmarkCommand> { }

        public sealed class BenchmarkHandler : BaseHandler, IBenchmarkQueryHandler, IBenchmarkCommandHandler
        {
            private static readonly NormalJsonModel model = NormalJsonModel.Create();
            public Task<NormalJsonModel> GetAsync(int id) => Task.FromResult(model);
            public Task Handle(BenchmarkCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private readonly BenchmarkHandler handler = new();
        private readonly BenchmarkCommand command = new() { Value = 1 };
        private readonly List<IBusSetup> buses = [];

        private IBus inProcess = null!;
        private IBenchmarkQueryHandler inProcessCaller = null!;
        private IBus tcp = null!;
        private IBenchmarkQueryHandler tcpCaller = null!;
        private IBenchmarkQueryHandler tcpEncryptedCaller = null!;

        [GlobalSetup]
        public void Setup()
        {
            var inProcessBus = Bus.New("inprocess");
            inProcessBus.AddHandler<IBenchmarkQueryHandler>(handler);
            inProcessBus.AddHandler<IBenchmarkCommandHandler>(handler);
            buses.Add(inProcessBus);
            inProcess = inProcessBus;
            inProcessCaller = inProcess.Call<IBenchmarkQueryHandler>();

            tcp = CreateTcp(null);
            tcpCaller = tcp.Call<IBenchmarkQueryHandler>();
            tcpEncryptedCaller = CreateTcp(new ZerraEncryptor(RandomNumberGenerator.GetBytes(32), SymmetricAlgorithmType.AES_GCM)).Call<IBenchmarkQueryHandler>();
        }

        private IBus CreateTcp(IEncryptor encryptor)
        {
            string url;
            using (var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = true;
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                url = $"http://localhost:{((IPEndPoint)socket.LocalEndPoint!).Port}";
            }
            var serializer = new ZerraByteSerializer();

            var server = Bus.New("server");
            server.AddHandler<IBenchmarkQueryHandler>(handler);
            server.AddHandler<IBenchmarkCommandHandler>(handler);
            var tcpServer = new TcpCqrsServer(url, serializer, encryptor, null, null);
            server.AddQueryServer<IBenchmarkQueryHandler>(tcpServer);
            server.AddCommandConsumer<IBenchmarkCommandHandler>(tcpServer);
            buses.Add(server);

            var client = Bus.New("client");
            var tcpClient = new TcpCqrsClient(url, serializer, encryptor, null, null);
            client.AddQueryClient<IBenchmarkQueryHandler>(tcpClient);
            client.AddCommandProducer<IBenchmarkCommandHandler>(tcpClient);
            buses.Add(client);
            return client;
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            foreach (var bus in buses)
                await bus.StopServicesAsync();
        }

        [Benchmark(Baseline = true)]
        public Task<NormalJsonModel> DirectCall() => handler.GetAsync(1);

        [Benchmark]
        public Task<NormalJsonModel> InProcessQuery() => inProcessCaller.GetAsync(1);

        [Benchmark]
        public Task InProcessCommand() => inProcess.DispatchAwaitAsync(command);

        [Benchmark]
        public Task<NormalJsonModel> TcpQuery() => tcpCaller.GetAsync(1);

        [Benchmark]
        public Task<NormalJsonModel> TcpQueryEncrypted() => tcpEncryptedCaller.GetAsync(1);

        [Benchmark]
        public Task TcpCommand() => tcp.DispatchAwaitAsync(command);

        //the time for all of them to finish is the slowest one's latency under that load
        [Benchmark]
        public Task TcpQuery64AtOnce()
        {
            var tasks = new Task[concurrent];
            for (var i = 0; i < concurrent; i++)
                tasks[i] = tcpCaller.GetAsync(i);
            return Task.WhenAll(tasks);
        }
    }
}
