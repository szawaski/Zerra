// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Reflection;
using Zerra.CQRS.Reflection.Dynamic;

namespace Zerra.Test.CQRS.Reflection
{
    public class BusReflectionTests
    {
        public sealed class InfoCommand : ICommand { }
        public sealed class InfoResultCommand : ICommand<int> { }
        public sealed class InfoEvent : IEvent { }
        public sealed class UnhandledCommand : ICommand { }
        public interface IInfoCommandHandler : ICommandHandler<InfoCommand>, ICommandHandler<InfoResultCommand, int> { }
        public interface IInfoEventHandler : IEventHandler<InfoEvent>, IDisposable { }

        public interface IRegisteredInfo { }
        public interface IRegisteredRouter { }
        public interface IRegisteredHandler { }
        public interface IRegisteredMetadata { }
        public sealed class RegisteredCommand : ICommand { }

        [Fact]
        public void GenerateMessageInfo_FindsTheInterfaceForACommandOrEvent()
        {
            var searched = new[] { typeof(IDisposable), typeof(IInfoEventHandler), typeof(IInfoCommandHandler) };

            var fromCommand = BusCommandOrEventInfoGenerator.GenerateMessageInfo(typeof(InfoResultCommand), searched);
            Assert.Equal(typeof(IInfoCommandHandler), fromCommand.InterfaceType);
            Assert.Equal(nameof(IInfoCommandHandler), fromCommand.InterfaceName);
            Assert.Equal([typeof(InfoCommand), typeof(InfoResultCommand)], fromCommand.CommandTypes);
            Assert.Empty(fromCommand.EventTypes);
            Assert.Equal("Handle-InfoCommand", fromCommand.HandleMethodNames[typeof(InfoCommand)]);

            var fromEvent = BusCommandOrEventInfoGenerator.GenerateMessageInfo(typeof(InfoEvent), searched);
            Assert.Equal(typeof(IInfoEventHandler), fromEvent.InterfaceType);
            Assert.Equal([typeof(InfoEvent)], fromEvent.EventTypes);
            Assert.Equal("Handle-InfoEvent", fromEvent.HandleMethodNames[typeof(InfoEvent)]);

            _ = Assert.ThrowsAny<Exception>(() => BusCommandOrEventInfoGenerator.GenerateMessageInfo(typeof(UnhandledCommand), searched));
            _ = Assert.ThrowsAny<Exception>(() => BusCommandOrEventInfoGenerator.GenerateMessageInfo(typeof(UnhandledCommand), null));
        }

        [Fact]
        public void GetByType_CachesUnderTheInterfaceAndItsMessages()
        {
            var info = BusCommandOrEventInfo.GetByType(typeof(InfoCommand), [typeof(IInfoCommandHandler)]);
            Assert.Same(info, BusCommandOrEventInfo.GetByType(typeof(InfoResultCommand), null));
            Assert.Same(info, BusCommandOrEventInfo.GetByInterfaceTypeOrNull(typeof(IInfoCommandHandler)));
            Assert.Equal(typeof(IInfoEventHandler), BusCommandOrEventInfo.GetByInterfaceTypeOrNull(typeof(IInfoEventHandler))!.InterfaceType);
        }

        [Fact]
        public void Register_RejectsDuplicatesAndClasses()
        {
            BusCommandOrEventInfo.Register(typeof(IRegisteredInfo), "Registered", [typeof(RegisteredCommand)], []);
            Assert.Equal("Registered", BusCommandOrEventInfo.GetByType(typeof(RegisteredCommand), null).InterfaceName);
            _ = Assert.Throws<ArgumentException>(() => BusCommandOrEventInfo.Register(typeof(IRegisteredInfo), "Registered", [], []));
            _ = Assert.Throws<ArgumentException>(() => BusCommandOrEventInfo.Register(typeof(RegisteredCommand), "Registered", [], []));

            BusRouters.Register(typeof(IRegisteredRouter), (bus, source) => source);
            Assert.Equal("source", BusRouters.GetBusCaller(typeof(IRegisteredRouter), null!, "source"));
            _ = Assert.Throws<InvalidOperationException>(() => BusRouters.Register(typeof(IRegisteredRouter), (bus, source) => source));
            _ = Assert.Throws<ArgumentException>(() => BusRouters.Register(typeof(RegisteredCommand), (bus, source) => source));

            BusRouters.Register(typeof(RegisteredCommand), (bus, command, type, source, cancellationToken) => Task.CompletedTask, task => null);
            Assert.NotNull(BusRouters.GetBusDispatcher(typeof(RegisteredCommand)));
            _ = Assert.Throws<InvalidOperationException>(() => BusRouters.Register(typeof(RegisteredCommand), (bus, command, type, source, cancellationToken) => Task.CompletedTask, task => null));

            BusHandlers.Register(typeof(IRegisteredHandler), "Run", false, null, [typeof(int)], (instance, args) => (int)args![0]! + 1, null);
            Assert.Equal(2, BusHandlers.Invoke(typeof(IRegisteredHandler), new object(), "Run", [1]));
            _ = Assert.Throws<InvalidOperationException>(() => BusHandlers.Register(typeof(IRegisteredHandler), "Run", false, null, [], (instance, args) => null, null));
            _ = Assert.Throws<ArgumentException>(() => BusHandlers.Register(typeof(RegisteredCommand), "Run", false, null, [], (instance, args) => null, null));

            var metadata = new HandlerMetadata(BusLogging.HandlerOnly);
            BusMetadata.Register(typeof(IRegisteredMetadata), metadata);
            Assert.Same(metadata, BusMetadata.GetByType(typeof(IRegisteredMetadata)));
            _ = Assert.Throws<InvalidOperationException>(() => BusMetadata.Register(typeof(IRegisteredMetadata), metadata));
        }
    }
}
