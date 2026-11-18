// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.OpenTelemetry;
using Zerra.Test.CQRS;

namespace Zerra.Test.OpenTelemetry
{
    //the tests share one ActivitySource and run one at a time within the class, each finds its own spans by its root's trace
    public class OpenTelemetryBusLoggerTests
    {
        private const int timeout = 30000;
        private static readonly NetworkByteSerializer serializer = new();
        private const string testSourceName = "Zerra.Test.OpenTelemetry";
        private static readonly ActivitySource testSource = new(testSourceName);

        private sealed class Spans : IDisposable
        {
            private readonly ActivityListener listener;
            private readonly ConcurrentQueue<Activity> stopped = new();

            public Spans()
            {
                listener = new ActivityListener()
                {
                    ShouldListenTo = x => x.Name == OpenTelemetryBusLogger.SourceName || x.Name == testSourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                    ActivityStopped = stopped.Enqueue
                };
                ActivitySource.AddActivityListener(listener);
            }

            public Activity[] Of(ActivityTraceId traceId) => stopped.Where(x => x.Source.Name == OpenTelemetryBusLogger.SourceName && x.TraceId == traceId).ToArray();

            public async Task<Activity> WaitFor(ActivityTraceId traceId, string name)
            {
                for (var i = 0; i < 200; i++)
                {
                    var span = Of(traceId).FirstOrDefault(x => x.DisplayName == name);
                    if (span is not null)
                        return span;
                    await Task.Delay(25);
                }
                throw new Exception($"No span {name}");
            }

            public void Dispose() => listener.Dispose();
        }

        private static (IBusSetup Server, IBusSetup Client) CreateBuses(TraceHandler handler, IBusLogger? clientLogger = null)
        {
            var url = TestNetwork.NewUrl();
            var server = Bus.New("server", null, new OpenTelemetryBusLogger(), null);
            server.AddHandler<ITraceQueryHandler>(handler);
            server.AddHandler<ITraceCommandHandler>(handler);
            server.AddHandler<ITraceEventHandler>(handler);
            var tcpServer = new TcpCqrsServer(url, serializer, null, null, null);
            server.AddQueryServer<ITraceQueryHandler>(tcpServer);
            server.AddCommandConsumer<ITraceCommandHandler>(tcpServer);
            server.AddEventConsumer<ITraceEventHandler>(tcpServer, EventConsumerMode.PerService);

            var client = Bus.New("client", null, clientLogger ?? new OpenTelemetryBusLogger(), null);
            var tcpClient = new TcpCqrsClient(url, serializer, null, null, null);
            client.AddQueryClient<ITraceQueryHandler>(tcpClient);
            client.AddCommandProducer<ITraceCommandHandler>(tcpClient);
            client.AddEventProducer<ITraceEventHandler>(tcpClient);
            return (server, client);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_HandlerSpanContinuesTheCallersTrace()
        {
            using var spans = new Spans();
            var handler = new TraceHandler();
            var (server, client) = CreateBuses(handler);
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    Assert.Equal(4, await client.Call<ITraceQueryHandler>().Double(2));
                }

                var all = spans.Of(traceId);
                var clientSpan = Assert.Single(all, x => x.Kind == ActivityKind.Client);
                var serverSpan = Assert.Single(all, x => x.Kind == ActivityKind.Server);
                Assert.Equal(clientSpan.SpanId, serverSpan.ParentSpanId);
                Assert.Equal(serverSpan.Id, handler.ActivityId);
                Assert.Equal("zerra", serverSpan.GetTagItem("rpc.system"));
                Assert.Equal(nameof(ITraceQueryHandler), serverSpan.GetTagItem("rpc.service"));
                Assert.Equal(nameof(ITraceQueryHandler.Double), serverSpan.GetTagItem("rpc.method"));
                Assert.Equal("server", serverSpan.GetTagItem("zerra.service"));
                Assert.Equal(ActivityStatusCode.Unset, serverSpan.Status);
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task CommandAndEvent_HandlerSpansContinueTheSendersTrace()
        {
            using var spans = new Spans();
            var handler = new TraceHandler();
            var (server, client) = CreateBuses(handler);
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    await client.DispatchAwaitAsync(new TraceCommand());
                    await client.DispatchAsync(new TraceEvent());
                }

                var commandSent = await spans.WaitFor(traceId, $"send {nameof(TraceCommand)}");
                var commandHandled = await spans.WaitFor(traceId, $"process {nameof(TraceCommand)}");
                var eventSent = await spans.WaitFor(traceId, $"send {nameof(TraceEvent)}");
                var eventHandled = await spans.WaitFor(traceId, $"process {nameof(TraceEvent)}");

                Assert.Equal(ActivityKind.Producer, commandSent.Kind);
                Assert.Equal(ActivityKind.Consumer, commandHandled.Kind);
                Assert.Equal(commandSent.SpanId, commandHandled.ParentSpanId);
                Assert.Equal("command", commandHandled.GetTagItem("zerra.message.kind"));
                Assert.Equal(nameof(TraceCommand), commandHandled.GetTagItem("messaging.destination.name"));
                Assert.Equal("process", commandHandled.GetTagItem("messaging.operation.type"));

                Assert.Equal(ActivityKind.Producer, eventSent.Kind);
                Assert.Equal(ActivityKind.Consumer, eventHandled.Kind);
                Assert.Equal(eventSent.SpanId, eventHandled.ParentSpanId);
                Assert.Equal("event", eventHandled.GetTagItem("zerra.message.kind"));
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task Failure_MarksBothSpansAsErrors()
        {
            using var spans = new Spans();
            var handler = new TraceHandler();
            var (server, client) = CreateBuses(handler);
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    _ = await Assert.ThrowsAnyAsync<Exception>(() => client.Call<ITraceQueryHandler>().Fail());
                }

                _ = await spans.WaitFor(traceId, $"{nameof(ITraceQueryHandler)}/{nameof(ITraceQueryHandler.Fail)}");
                var all = spans.Of(traceId);
                Assert.Equal(2, all.Length);
                foreach (var span in all)
                    Assert.Equal(ActivityStatusCode.Error, span.Status);
                Assert.Equal(typeof(InvalidOperationException).FullName, Assert.Single(all, x => x.Kind == ActivityKind.Server).GetTagItem("error.type"));
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task WithoutACurrentActivity_TheSenderStartsTheTrace()
        {
            using var spans = new Spans();
            var handler = new TraceHandler();
            var (server, client) = CreateBuses(handler);
            try
            {
                Assert.Null(Activity.Current);
                Assert.Equal(4, await client.Call<ITraceQueryHandler>().Double(2));

                var serverSpan = ServerSpan(spans, handler);
                var clientSpan = Assert.Single(spans.Of(serverSpan.TraceId), x => x.Kind == ActivityKind.Client);
                Assert.Equal(default, clientSpan.ParentSpanId);
                Assert.Equal(clientSpan.SpanId, serverSpan.ParentSpanId);
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        private static Activity ServerSpan(Spans spans, TraceHandler handler)
        {
            Assert.NotNull(handler.ActivityId);
            var traceId = ActivityTraceId.CreateFromString(handler.ActivityId.AsSpan(3, 32));
            return Assert.Single(spans.Of(traceId), x => x.Kind == ActivityKind.Server);
        }

        [Fact(Timeout = timeout)]
        public async Task ServerStartedInsideAnActivity_HandlerSpanStillContinuesTheCallersTrace()
        {
            using var spans = new Spans();
            var handler = new TraceHandler();
            IBusSetup server;
            IBusSetup client;
            using (testSource.StartActivity("startup"))
            {
                (server, client) = CreateBuses(handler);
                Assert.Equal(2, await client.Call<ITraceQueryHandler>().Double(1));
            }
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    Assert.Equal(4, await client.Call<ITraceQueryHandler>().Double(2));
                }

                var all = spans.Of(traceId);
                var clientSpan = Assert.Single(all, x => x.Kind == ActivityKind.Client);
                var serverSpan = Assert.Single(all, x => x.Kind == ActivityKind.Server);
                Assert.Equal(clientSpan.SpanId, serverSpan.ParentSpanId);
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task ServiceWithoutTracing_PassesTheTraceOn()
        {
            //caller (traced) -> relay (no tracing) -> server (traced), the server's span continues the caller's trace
            using var spans = new Spans();
            var handler = new TraceHandler();
            var serverUrl = TestNetwork.NewUrl();
            var server = Bus.New("server", null, new OpenTelemetryBusLogger(), null);
            server.AddHandler<ITraceQueryHandler>(handler);
            server.AddQueryServer<ITraceQueryHandler>(new TcpCqrsServer(serverUrl, serializer, null, null, null));

            var relayUrl = TestNetwork.NewUrl();
            var relay = Bus.New("relay", null, null, null);
            relay.AddHandler<ITraceRelayQueryHandler>(new TraceRelayHandler());
            relay.AddQueryServer<ITraceRelayQueryHandler>(new TcpCqrsServer(relayUrl, serializer, null, null, null));
            relay.AddQueryClient<ITraceQueryHandler>(new TcpCqrsClient(serverUrl, serializer, null, null, null));

            var client = Bus.New("client", null, new OpenTelemetryBusLogger(), null);
            client.AddQueryClient<ITraceRelayQueryHandler>(new TcpCqrsClient(relayUrl, serializer, null, null, null));
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    Assert.Equal(6, await client.Call<ITraceRelayQueryHandler>().Relay(3));
                }

                var serverSpan = await spans.WaitFor(traceId, $"{nameof(ITraceQueryHandler)}/{nameof(ITraceQueryHandler.Double)}");
                var clientSpan = await spans.WaitFor(traceId, $"{nameof(ITraceRelayQueryHandler)}/{nameof(ITraceRelayQueryHandler.Relay)}");
                Assert.Equal(ActivityKind.Server, serverSpan.Kind);
                Assert.Equal(clientSpan.SpanId, serverSpan.ParentSpanId);
            }
            finally
            {
                await client.StopServicesAsync();
                await relay.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task LocalCallInsideAHandler_IsAChildSpan()
        {
            using var spans = new Spans();
            var bus = Bus.New("local", null, new OpenTelemetryBusLogger(), null);
            bus.AddHandler<ITraceQueryHandler>(new TraceHandler());
            bus.AddHandler<ITraceRelayQueryHandler>(new TraceRelayHandler());
            try
            {
                ActivityTraceId traceId;
                using (var root = testSource.StartActivity("root")!)
                {
                    traceId = root.TraceId;
                    Assert.Equal(6, await bus.Call<ITraceRelayQueryHandler>().Relay(3));
                }

                var outer = await spans.WaitFor(traceId, $"{nameof(ITraceRelayQueryHandler)}/{nameof(ITraceRelayQueryHandler.Relay)}");
                var inner = await spans.WaitFor(traceId, $"{nameof(ITraceQueryHandler)}/{nameof(ITraceQueryHandler.Double)}");
                Assert.Equal(outer.SpanId, inner.ParentSpanId);
                Assert.Equal(ActivityKind.Server, outer.Kind);
            }
            finally
            {
                await bus.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task OtherBusLogger_IsCalledToo()
        {
            var recording = new BusRoutingTests.RecordingBusLogger();
            var bus = Bus.New("local", null, new OpenTelemetryBusLogger(recording), null);
            bus.AddHandler<ITraceQueryHandler>(new TraceHandler());
            bus.AddHandler<ITraceCommandHandler>(new TraceHandler());
            try
            {
                Assert.Equal(4, await bus.Call<ITraceQueryHandler>().Double(2));
                await bus.DispatchAwaitAsync(new TraceCommand());

                Assert.Single(recording.Entries, x => x.Kind == "Call" && x.Handled);
                Assert.Single(recording.Entries, x => x.Kind == "Command" && x.Handled);
            }
            finally
            {
                await bus.StopServicesAsync();
            }
        }

        [Fact(Timeout = timeout)]
        public async Task Durations_AreRecorded()
        {
            var measurements = new ConcurrentQueue<(double Seconds, Dictionary<string, object?> Tags)>();
            using var listener = new MeterListener()
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == OpenTelemetryBusLogger.SourceName)
                        meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
            {
                var tagDictionary = new Dictionary<string, object?>();
                foreach (var tag in tags)
                    tagDictionary[tag.Key] = tag.Value;
                measurements.Enqueue((value, tagDictionary));
            });
            listener.Start();

            var bus = Bus.New("local", null, new OpenTelemetryBusLogger(), null);
            bus.AddHandler<ITraceQueryHandler>(new TraceHandler());
            try
            {
                _ = await bus.Call<ITraceQueryHandler>().Double(2);
                _ = await Assert.ThrowsAnyAsync<Exception>(() => bus.Call<ITraceQueryHandler>().Fail());

                var ok = Assert.Single(measurements, x => Equals(x.Tags["rpc.method"], nameof(ITraceQueryHandler.Double)));
                Assert.Equal(nameof(ITraceQueryHandler), ok.Tags["rpc.service"]);
                Assert.Equal(true, ok.Tags["zerra.handled"]);
                Assert.False(ok.Tags.ContainsKey("error.type"));
                Assert.True(ok.Seconds >= 0);

                var failed = Assert.Single(measurements, x => Equals(x.Tags["rpc.method"], nameof(ITraceQueryHandler.Fail)));
                Assert.Equal(typeof(InvalidOperationException).FullName, failed.Tags["error.type"]);
            }
            finally
            {
                await bus.StopServicesAsync();
            }
        }

        public sealed class TraceCommand : ICommand { }
        public sealed class TraceEvent : IEvent { }
        public interface ITraceCommandHandler : ICommandHandler<TraceCommand> { }
        public interface ITraceEventHandler : IEventHandler<TraceEvent> { }
        public interface ITraceQueryHandler : IQueryHandler
        {
            Task<int> Double(int value);
            Task<int> Fail();
        }
        public interface ITraceRelayQueryHandler : IQueryHandler
        {
            Task<int> Relay(int value);
        }

        public sealed class TraceHandler : BaseHandler, ITraceQueryHandler, ITraceCommandHandler, ITraceEventHandler
        {
            public string? ActivityId;

            public Task<int> Double(int value)
            {
                ActivityId = Activity.Current?.Id;
                return Task.FromResult(value * 2);
            }

            public Task<int> Fail() => throw new InvalidOperationException("Failed");

            public Task Handle(TraceCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task Handle(TraceEvent @event) => Task.CompletedTask;
        }

        public sealed class TraceRelayHandler : BaseHandler, ITraceRelayQueryHandler
        {
            public Task<int> Relay(int value) => Bus.Call<ITraceQueryHandler>().Double(value);
        }
    }
}
