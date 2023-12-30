// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        void BeginCommand(Type commandType, ICommand command, string source, bool handled);
        void BeginEvent(Type eventType, IEvent @event, string source, bool handled);
        void BeginCall(Type interfaceType, string methodName, object[] arguments, object? result, string source, bool handled);

        void EndCommand(Type commandType, ICommand command, string source, bool handled, long milliseconds, Exception? ex);
        void EndEvent(Type eventType, IEvent @event, string source, bool handled, long milliseconds, Exception? ex);
        void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string source, bool handled, long milliseconds, Exception? ex);
    }
}