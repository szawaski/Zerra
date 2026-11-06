// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Reflection;
using Zerra.CQRS.Reflection.Dynamic;
using Zerra.Encryption;

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
        public interface IInfoQueryHandler : IQueryHandler
        {
            Task<int> GetValue();
        }

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
        public void GenerateMethodForHandler_SkipsIHandler()
        {
            var handle = BusHandlerGenerator.GenerateMethodForHandler(typeof(IInfoCommandHandler), "Handle-InfoCommand");
            Assert.Equal("Handle", handle.Name);

            //Initialize sets up the handler, a remote caller must not reach it
            _ = Assert.Throws<ArgumentException>(() => BusHandlerGenerator.GenerateMethodForHandler(typeof(IInfoCommandHandler), nameof(IHandler.Initialize)));
            _ = Assert.Throws<ArgumentException>(() => BusHandlers.GetMethod(typeof(IInfoQueryHandler), nameof(IHandler.Initialize)));
            Assert.Equal(nameof(IInfoQueryHandler.GetValue), BusHandlers.GetMethod(typeof(IInfoQueryHandler), nameof(IInfoQueryHandler.GetValue)).Name);
        }

        [Fact]
        public void GetByType_CachesUnderTheInterfaceAndItsMessages()
        {
            var info = BusCommandOrEventInfo.GetByType(typeof(InfoCommand), [typeof(IInfoCommandHandler)]);
            Assert.Same(info, BusCommandOrEventInfo.GetByType(typeof(InfoResultCommand), null));
            Assert.Same(info, BusCommandOrEventInfo.GetByInterfaceTypeOrNull(typeof(IInfoCommandHandler)));
            Assert.Equal(typeof(IInfoEventHandler), BusCommandOrEventInfo.GetByInterfaceTypeOrNull(typeof(IInfoEventHandler))!.InterfaceType);
        }

        public interface IDynamicCaller
        {
            int Get(int value);
            Task Run(int value);
            Task<int> GetAsync(int value);
        }

        [Fact]
        public async Task GenerateBusCaller_PassesTheOverrides()
        {
            var creator = BusRouterGenerator.GenerateBusCaller(typeof(IDynamicCaller));
            var bus = new RecordingBusInternal();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES_GCM);

            var caller = (IDynamicCaller)creator(bus, "source", true, "10.1.4.7:9001", encryptor);
            Assert.Equal(1, caller.Get(1));
            await caller.Run(2);
            Assert.Equal(3, await caller.GetAsync(3));
            Assert.Equal([("Get", "source", true, "10.1.4.7:9001", encryptor), ("Run", "source", true, "10.1.4.7:9001", encryptor), ("GetAsync", "source", true, "10.1.4.7:9001", encryptor)], bus.Calls);

            bus.Calls.Clear();
            caller = (IDynamicCaller)creator(bus, "source", false, null, null);
            _ = caller.Get(1);
            Assert.Equal([("Get", "source", false, (string?)null, (IEncryptor?)null)], bus.Calls);
        }

        private sealed class RecordingBusInternal : IBusInternal
        {
            public readonly List<(string Method, string Source, bool Remote, string? OverrideServiceUrl, IEncryptor? OverrideEncryptor)> Calls = new();

            public TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, string source, bool remote, string? overrideServiceUrl, IEncryptor? overrideEncryptor)
            {
                Calls.Add((methodName, source, remote, overrideServiceUrl, overrideEncryptor));
                return (TReturn)arguments[0];
            }
            public Task _CallMethodTask(Type interfaceType, string methodName, object[] arguments, string source, bool remote, string? overrideServiceUrl, IEncryptor? overrideEncryptor)
            {
                Calls.Add((methodName, source, remote, overrideServiceUrl, overrideEncryptor));
                return Task.CompletedTask;
            }
            public Task<TReturn> _CallMethodTaskGeneric<TReturn>(Type interfaceType, string methodName, object[] arguments, string source, bool remote, string? overrideServiceUrl, IEncryptor? overrideEncryptor)
            {
                Calls.Add((methodName, source, remote, overrideServiceUrl, overrideEncryptor));
                return Task.FromResult((TReturn)arguments[0]);
            }
            public Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, string source, bool remote, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task<TResult> _DispatchCommandWithResultInternalAsync<TResult>(ICommand<TResult> command, Type commandType, string source, bool remote, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task _DispatchEventInternalAsync(IEvent @event, Type eventType, string source, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        [Fact]
        public void Register_RejectsDuplicatesAndClasses()
        {
            BusCommandOrEventInfo.Register(typeof(IRegisteredInfo), "Registered", [typeof(RegisteredCommand)], []);
            Assert.Equal("Registered", BusCommandOrEventInfo.GetByType(typeof(RegisteredCommand), null).InterfaceName);
            _ = Assert.Throws<ArgumentException>(() => BusCommandOrEventInfo.Register(typeof(IRegisteredInfo), "Registered", [], []));
            _ = Assert.Throws<ArgumentException>(() => BusCommandOrEventInfo.Register(typeof(RegisteredCommand), "Registered", [], []));

            BusRouters.Register(typeof(IRegisteredRouter), (bus, source, remote, overrideServiceUrl, overrideEncryptor) => source);
            Assert.Equal("source", BusRouters.GetBusCaller(typeof(IRegisteredRouter), null!, "source", false, null, null));
            _ = Assert.Throws<InvalidOperationException>(() => BusRouters.Register(typeof(IRegisteredRouter), (bus, source, remote, overrideServiceUrl, overrideEncryptor) => source));
            _ = Assert.Throws<ArgumentException>(() => BusRouters.Register(typeof(RegisteredCommand), (bus, source, remote, overrideServiceUrl, overrideEncryptor) => source));

            BusRouters.Register(typeof(RegisteredCommand), (bus, command, type, source, remote, cancellationToken) => Task.CompletedTask, task => null);
            Assert.NotNull(BusRouters.GetBusDispatcher(typeof(RegisteredCommand)));
            _ = Assert.Throws<InvalidOperationException>(() => BusRouters.Register(typeof(RegisteredCommand), (bus, command, type, source, remote, cancellationToken) => Task.CompletedTask, task => null));

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
