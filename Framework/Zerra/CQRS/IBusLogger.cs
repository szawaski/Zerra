// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Methods to implement <see cref="Bus"/> logging. The implementaion must be registered with the <see cref="Bus"/> using <see cref="Bus.AddLogger(IBusLogger)"/>
    /// </summary>
    public interface IBusLogger
    {
        void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled);
        void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled);
        void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled);

        void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex);
        void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex);
        void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex);
    }
}
