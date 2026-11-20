// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Map;
using Zerra.OpenTelemetry;
using Zerra.Repository;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;

namespace Zerra.AotTest
{
    //Runs the main features the way an application does, published as Native AOT so everything comes from the generated code
    public static class Program
    {
        private static int failures;

        private static void Check(string name, bool passed)
        {
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}");
            if (!passed)
                failures++;
        }

        private static async Task Run(string name, Func<Task> test)
        {
            try
            {
                await test().WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                Check($"{name} threw {ex.GetType().Name}: {ex.Message}", false);
            }
        }

        public static async Task<int> Main()
        {
            Console.WriteLine($"Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}");

            await Run("Serializers", Serializers);
            await Run("Mapper", Mapper);
            await Run("Members", Members);
            await Run("Encryption", Encryption);
            await Run("Bus over TCP", () => BusOver(false));
            await Run("Bus over HTTP", () => BusOver(true));
            await Run("Repository", Repository);

            Console.WriteLine(failures == 0 ? "All checks passed" : $"{failures} checks failed");
            return failures == 0 ? 0 : 1;
        }

        private static Order NewOrder() => new()
        {
            ID = Guid.NewGuid(),
            Number = 42,
            Customer = "Ada",
            Total = 12.34m,
            Placed = new DateTime(2026, 11, 18, 10, 30, 0, DateTimeKind.Utc),
            Status = OrderStatus.Shipped,
            Notes = null,
            Lines = [new OrderLine() { Sku = "A1", Quantity = 2 }, new OrderLine() { Sku = "B2", Quantity = 1 }],
            Tags = new Dictionary<string, int>() { ["gift"] = 1 },
            Address = new Address("1 Main St", "Springfield")
        };

        private static bool Same(Order expected, Order? actual) =>
            actual is not null && actual.ID == expected.ID && actual.Number == expected.Number && actual.Customer == expected.Customer && actual.Total == expected.Total
            && actual.Placed == expected.Placed && actual.Status == expected.Status && actual.Notes == expected.Notes
            && actual.Lines is not null && actual.Lines.Count == expected.Lines!.Count && actual.Lines[1].Sku == "B2" && actual.Lines[0].Quantity == 2
            && actual.Tags is not null && actual.Tags["gift"] == 1 && actual.Address == expected.Address;

        private static Task Serializers()
        {
            var order = NewOrder();
            Check("ByteSerializer round trip", Same(order, ByteSerializer.Deserialize<Order>(ByteSerializer.Serialize(order))));
            Check("JsonSerializer round trip", Same(order, JsonSerializer.Deserialize<Order>(JsonSerializer.Serialize(order))));
            Check("JsonSerializer nameless round trip", Same(order, JsonSerializer.Deserialize<Order>(JsonSerializer.Serialize(order, new JsonSerializerOptions() { Nameless = true }), new JsonSerializerOptions() { Nameless = true })));

            //a type only ever serialized, not part of any contract, is found from the calls
            var entry = new CacheEntry() { Key = "orders", Hits = 3, Expires = new CacheExpiry() { Minutes = 5 } };
            var fromBytes = ByteSerializer.Deserialize<CacheEntry>(ByteSerializer.Serialize(entry));
            var fromJson = JsonSerializer.Deserialize<CacheEntry>(JsonSerializer.Serialize(entry));
            Check("Serializer finds types only used with it", fromBytes?.Key == "orders" && fromBytes.Expires?.Minutes == 5 && fromJson?.Hits == 3 && fromJson.Expires?.Minutes == 5);
            return Task.CompletedTask;
        }

        private static Task Members()
        {
            //init and required init setters are called through UnsafeAccessor, an internal setter directly
            var init = new InitModel() { Name = "Ada", Count = 2, Internal = "i" };
            var initJson = JsonSerializer.Deserialize<InitModel>(JsonSerializer.Serialize(init));
            var initBytes = ByteSerializer.Deserialize<InitModel>(ByteSerializer.Serialize(init));
            Check("Init and required members", initJson?.Name == "Ada" && initJson.Count == 2 && initJson.Internal == "i" && initBytes?.Name == "Ada" && initBytes.Count == 2);

            //a generic type's init setter can't use an UnsafeAccessor, it's a delegate found by name, which trimming has to keep
            var wrapper = JsonSerializer.Deserialize<InitWrapper<int>>(JsonSerializer.Serialize(new InitWrapper<int>() { Value = 5 }));
            Check("Generic init member", wrapper?.Value == 5);

            var points = new List<Point>() { new() { X = 1, Y = 2 }, new() { X = 3, Y = 4 } };
            var pointsJson = JsonSerializer.Deserialize<List<Point>>(JsonSerializer.Serialize(points));
            var pointBytes = ByteSerializer.Deserialize<Point>(ByteSerializer.Serialize(points[1]));
            Check("Struct models", pointsJson is not null && pointsJson[0].X == 1 && pointsJson[1].Y == 4 && pointBytes.X == 3 && pointBytes.Y == 4);

            //ref and out arguments are written back into the arguments array
            var constructor = Zerra.Reflection.TypeAnalyzer<RefOutModel>.GetTypeDetail().Constructors.Single(x => x.Parameters.Count == 2);
            var args = new object?[] { 5, null };
            var refOut = constructor.Creator(args);
            Check("Ref and out constructor", refOut.Value == 5 && (int)args[0]! == 10 && (string)args[1]! == "out");

            Check("Full access needs dynamic code", FullAccessThrows());
            return Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Checks that full access refuses to run without dynamic code")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Checks that full access refuses to run without dynamic code")]
        private static bool FullAccessThrows()
        {
            try
            {
                _ = Zerra.Reflection.TypeAnalyzer<InitModel>.GetTypeDetail().GetFullAccess();
                return false;
            }
            catch (NotSupportedException)
            {
                return true;
            }
        }

        private static Task Mapper()
        {
            var order = NewOrder();
            var summary = order.Map<OrderSummary>();
            Check("Mapper", summary.ID == order.ID && summary.Customer == order.Customer && summary.Total == order.Total && summary.Status == order.Status);
            return Task.CompletedTask;
        }

        private static Task Encryption()
        {
            var encryptor = new ZerraEncryptor("aot-test-key", SymmetricAlgorithmType.AES_GCM);
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            Check("Encryption round trip", encryptor.Decrypt(encryptor.Encrypt(bytes)).SequenceEqual(bytes));
            return Task.CompletedTask;
        }

        private static int FreePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        private static async Task BusOver(bool http)
        {
            var name = http ? "HTTP" : "TCP";
            var spans = new List<Activity>();
            using var listener = new ActivityListener()
            {
                ShouldListenTo = x => x.Name == OpenTelemetryBusLogger.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = x => { lock (spans) spans.Add(x); }
            };
            ActivitySource.AddActivityListener(listener);

            var url = $"127.0.0.1:{FreePort()}";
            var serializer = new NetworkByteSerializer();
            var encryptor = new ZerraEncryptor("aot-test-key", SymmetricAlgorithmType.AES_GCM);
            var handler = new OrderHandler();

            var server = Bus.New("OrderService", null, new OpenTelemetryBusLogger(), null);
            server.AddHandler<IOrderQueryHandler>(handler);
            server.AddHandler<IOrderCommandHandler>(handler);
            server.AddHandler<IOrderEventHandler>(handler);
            CqrsServerBase cqrsServer = http
                ? new HttpCqrsServer(url, serializer, encryptor, null, null, null)
                : new TcpCqrsServer(url, serializer, encryptor, null, null);
            server.AddQueryServer<IOrderQueryHandler>(cqrsServer);
            server.AddCommandConsumer<IOrderCommandHandler>(cqrsServer);
            server.AddEventConsumer<IOrderEventHandler>(cqrsServer, EventConsumerMode.PerService);

            var client = Bus.New("WebService", null, new OpenTelemetryBusLogger(), null);
            CqrsClientBase cqrsClient = http
                ? new HttpCqrsClient(url, serializer, encryptor, null, null, null)
                : new TcpCqrsClient(url, serializer, encryptor, null, null);
            client.AddQueryClient<IOrderQueryHandler>(cqrsClient);
            client.AddCommandProducer<IOrderCommandHandler>(cqrsClient);
            client.AddEventProducer<IOrderEventHandler>(cqrsClient);

            try
            {
                var order = NewOrder();
                Check($"{name} query", Same(order, await client.Call<IOrderQueryHandler>().Echo(order)));
                await client.DispatchAwaitAsync(new PlaceOrderCommand() { Order = order });
                Check($"{name} command", handler.Placed?.ID == order.ID);
                Check($"{name} command with result", await client.DispatchAwaitAsync(new CountOrdersCommand() { Customer = "Ada" }) == 3);
                await client.DispatchAsync(new OrderShippedEvent() { OrderID = order.ID });
                Check($"{name} event", await handler.Shipped.Task.WaitAsync(TimeSpan.FromSeconds(10)) == order.ID);

                Activity[] recorded;
                lock (spans)
                    recorded = spans.ToArray();
                var clientSpan = recorded.FirstOrDefault(x => x.Kind == ActivityKind.Client);
                var serverSpan = recorded.FirstOrDefault(x => x.Kind == ActivityKind.Server);
                Check($"{name} tracing continues the caller's trace", clientSpan is not null && serverSpan is not null && serverSpan.TraceId == clientSpan.TraceId && serverSpan.ParentSpanId == clientSpan.SpanId);
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        private static async Task Repository()
        {
            var engine = new Zerra.Repository.Memory.MemoryEngine();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns, [typeof(ProductDataModel)]);
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ProductDataModel>(engine));

            await repo.CreateAsync(new ProductDataModel() { ID = Guid.NewGuid(), Name = "Kettle", Price = 25m, Stock = 3 });
            await repo.CreateAsync(new ProductDataModel() { ID = Guid.NewGuid(), Name = "Mug", Price = 8m, Stock = 0 });
            await repo.CreateAsync(new ProductDataModel() { ID = Guid.NewGuid(), Name = "Teapot", Price = 30m, Stock = 1 });

            var inStock = await repo.Query<ProductDataModel>().Where(x => x.Stock > 0).OrderBy(x => x.Name).ToArrayAsync();
            Check("Repository query", inStock.Select(x => x.Name).SequenceEqual(["Kettle", "Teapot"]));

            var mug = inStock.Length == 2 ? (await repo.Query<ProductDataModel>().Where(x => x.Name == "Mug").ToArrayAsync()).Single() : null;
            if (mug is not null)
            {
                mug.Stock = 5;
                await repo.UpdateAsync(mug);
            }
            Check("Repository update", await repo.Query<ProductDataModel>().CountAsync() == 3 && (await repo.Query<ProductDataModel>().Where(x => x.Stock > 0).ToArrayAsync()).Length == 3);
        }
    }

    public enum OrderStatus { Placed, Shipped }

    public sealed class CacheEntry
    {
        public string? Key { get; set; }
        public int Hits { get; set; }
        public CacheExpiry? Expires { get; set; }
    }

    public sealed class CacheExpiry
    {
        public int Minutes { get; set; }
    }

    public sealed class Order
    {
        public Guid ID { get; set; }
        public int Number { get; set; }
        public string? Customer { get; set; }
        public decimal Total { get; set; }
        public DateTime Placed { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public List<OrderLine>? Lines { get; set; }
        public Dictionary<string, int>? Tags { get; set; }
        public Address? Address { get; set; }
    }

    public sealed class OrderLine
    {
        public string? Sku { get; set; }
        public int Quantity { get; set; }
    }

    public sealed record Address(string Street, string City);

    public sealed class InitModel
    {
        public required string Name { get; init; }
        public int Count { get; init; }
        public string? Internal { get; internal set; }
    }

    public sealed class InitWrapper<T>
    {
        public T? Value { get; init; }
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Zerra.Reflection.GenerateTypeDetail]
    public sealed class RefOutModel
    {
        public int Value { get; }

        public RefOutModel(ref int value, out string text)
        {
            Value = value;
            value *= 2;
            text = "out";
        }
    }

    public sealed class OrderSummary
    {
        public Guid ID { get; set; }
        public string? Customer { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }
    }

    public interface IOrderQueryHandler : IQueryHandler
    {
        Task<Order> Echo(Order order);
    }

    public sealed class PlaceOrderCommand : ICommand
    {
        public Order? Order { get; set; }
    }

    public sealed class CountOrdersCommand : ICommand<int>
    {
        public string? Customer { get; set; }
    }

    public interface IOrderCommandHandler : ICommandHandler<PlaceOrderCommand>, ICommandHandler<CountOrdersCommand, int> { }

    public sealed class OrderShippedEvent : IEvent
    {
        public Guid OrderID { get; set; }
    }

    public interface IOrderEventHandler : IEventHandler<OrderShippedEvent> { }

    public sealed class OrderHandler : BaseHandler, IOrderQueryHandler, IOrderCommandHandler, IOrderEventHandler
    {
        public Order? Placed;
        public TaskCompletionSource<Guid> Shipped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Order> Echo(Order order) => Task.FromResult(order);

        public Task Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
        {
            Placed = command.Order;
            return Task.CompletedTask;
        }

        public Task<int> Handle(CountOrdersCommand command, CancellationToken cancellationToken) => Task.FromResult(command.Customer == "Ada" ? 3 : 0);

        public Task Handle(OrderShippedEvent @event)
        {
            _ = Shipped.TrySetResult(@event.OrderID);
            return Task.CompletedTask;
        }
    }

    [Entity("Product")]
    public sealed class ProductDataModel
    {
        [Identity]
        public Guid ID { get; set; }

        [StoreProperties(true, 64)]
        public string? Name { get; set; }

        [StoreProperties(18, 2)]
        public decimal Price { get; set; }

        public int Stock { get; set; }
    }
}
