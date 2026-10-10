// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#pragma warning disable CS0618 //tests the obsolete static Bus

using Xunit;
using Zerra.CQRS;

namespace Zerra.Test.CQRS
{
    //the static Bus is the last one created, so nothing else may create a bus while these run
    [Collection(nameof(StaticBusCollection))]
    public class BusStaticTests
    {
        [Fact]
        public async Task StaticMethods_UseTheLastBusCreated()
        {
            var handler = new BusRoutingTests.RoutingHandler();
            var bus = Bus.New("static", null, null, null);
            bus.AddHandler<BusRoutingTests.IRoutingCommandHandler>(handler);
            bus.AddHandler<BusRoutingTests.IRoutingEventHandler>(handler);
            bus.AddHandler<BusRoutingTests.IRoutingQueryHandler>(handler);

            await Bus.DispatchAsync(new BusRoutingTests.RoutingCommand { Value = 1 });
            await Bus.DispatchAwaitAsync(new BusRoutingTests.RoutingCommand { Value = 2 });
            Assert.Equal(6, await Bus.DispatchAwaitAsync(new BusRoutingTests.RoutingCommandWithResult { Value = 3 }));
            await Bus.DispatchAsync(new BusRoutingTests.RoutingEvent { Value = 4 });
            await Bus.DispatchAsync(new BusRoutingTests.RoutingCommand { Value = 5 }, TimeSpan.FromSeconds(10));
            await Bus.DispatchAwaitAsync(new BusRoutingTests.RoutingCommand { Value = 6 }, TimeSpan.FromSeconds(10));
            Assert.Equal(14, await Bus.DispatchAwaitAsync(new BusRoutingTests.RoutingCommandWithResult { Value = 7 }, TimeSpan.FromSeconds(10)));
            await Bus.DispatchAsync(new BusRoutingTests.RoutingEvent { Value = 8 }, TimeSpan.FromSeconds(10));
            Assert.Equal(18, Bus.Call<BusRoutingTests.IRoutingQueryHandler>().Double(9));

            await handler.Wait(8);
            Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], handler.Received.OrderBy(x => x));
        }
    }

    [CollectionDefinition(nameof(StaticBusCollection), DisableParallelization = true)]
    public sealed class StaticBusCollection { }
}
