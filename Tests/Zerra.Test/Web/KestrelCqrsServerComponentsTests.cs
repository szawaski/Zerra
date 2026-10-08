// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class KestrelCqrsServerComponentsTests
    {
        public static class First
        {
            public interface ISameName : IQueryHandler { }
            public sealed class SameCommand : ICommand { }
            public sealed class SameEvent : IEvent { }
        }
        public static class Second
        {
            public interface ISameName : IQueryHandler { }
            public sealed class SameCommand : ICommand { }
            public sealed class SameEvent : IEvent { }
        }

        private static KestrelCqrsServerLinkedSettings Settings() => new(null, null, ContentType.Bytes);

        [Fact]
        public async Task QueryServer_RegistersOnceAndRejectsSameNames()
        {
            var settings = Settings();
            var server = new KestrelCqrsServerQueryServer(settings);
            IQueryServer queryServer = server;
            Assert.Equal("[Kestrel Host]", queryServer.ServiceUrl);
            server.Open();
            server.Close();
            server.Dispose();
            await server.DisposeAsync();

            queryServer.RegisterInterfaceType(1, typeof(First.ISameName));
            queryServer.RegisterInterfaceType(5, typeof(First.ISameName));
            Assert.Equal(typeof(First.ISameName), settings.Types[nameof(First.ISameName)].Type);
            _ = Assert.Throws<InvalidOperationException>(() => queryServer.RegisterInterfaceType(1, typeof(Second.ISameName)));
        }

        [Fact]
        public async Task CommandConsumer_RegistersOnceAndSetsUpOnce()
        {
            var settings = Settings();
            var consumer = new KestrelCqrsServerCommandConsumer(settings);
            ICommandConsumer commandConsumer = consumer;
            Assert.Equal("[Kestrel Host]", commandConsumer.MessageHost);
            consumer.Open();
            consumer.Close();
            consumer.Dispose();
            await consumer.DisposeAsync();

            commandConsumer.RegisterCommandType(1, "topic", typeof(First.SameCommand));
            commandConsumer.RegisterCommandType(1, "topic", typeof(First.SameCommand));
            _ = Assert.Throws<InvalidOperationException>(() => commandConsumer.RegisterCommandType(1, "topic", typeof(Second.SameCommand)));

            HandleRemoteCommandDispatch handler = (_, _, _) => Task.CompletedTask;
            HandleRemoteCommandWithResultDispatch withResult = (_, _, _) => Task.FromResult<object?>(null);
            commandConsumer.Setup(null, handler, handler, withResult);
            commandConsumer.Setup(null, handler, handler, withResult); //the same bus again
            _ = Assert.Throws<InvalidOperationException>(() => commandConsumer.Setup(null, (_, _, _) => Task.CompletedTask, handler, withResult));
            _ = Assert.Throws<InvalidOperationException>(() => commandConsumer.Setup(new CommandCounter(1, null), handler, handler, withResult));
        }

        [Fact]
        public async Task EventConsumer_RegistersOnceAndSetsUpOnce()
        {
            var settings = Settings();
            var consumer = new KestrelCqrsServerEventConsumer(settings);
            IEventConsumer eventConsumer = consumer;
            Assert.Equal("[Kestrel Host]", eventConsumer.MessageHost);
            consumer.Open();
            consumer.Close();
            consumer.Dispose();
            await consumer.DisposeAsync();

            eventConsumer.RegisterEventType(1, "topic", typeof(First.SameEvent), EventConsumerMode.PerService);
            eventConsumer.RegisterEventType(1, "topic", typeof(First.SameEvent), EventConsumerMode.PerService);
            _ = Assert.Throws<InvalidOperationException>(() => eventConsumer.RegisterEventType(1, "topic", typeof(Second.SameEvent), EventConsumerMode.PerService));

            HandleRemoteEventDispatch handler = (_, _) => Task.CompletedTask;
            eventConsumer.Setup("service", handler);
            eventConsumer.Setup("service", handler);
            _ = Assert.Throws<InvalidOperationException>(() => eventConsumer.Setup("service", (_, _) => Task.CompletedTask));
        }
    }
}
