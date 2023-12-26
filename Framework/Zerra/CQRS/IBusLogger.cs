// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        void BeginLogCommandAsync(Type commandType, ICommand command, string source, bool handled);
        void BeginLogEventAsync(Type eventType, IEvent @event, string source, bool handled);
        void BeginLogCallAsync(Type interfaceType, string methodName, object[] arguments, object result, string source, bool handled);

        void LogCommandAsync(Type commandType, ICommand command, string source, bool handled, long milliseconds, Exception ex);
        void LogEventAsync(Type eventType, IEvent @event, string source, bool handled, long milliseconds, Exception ex);
        void LogCallAsync(Type interfaceType, string methodName, object[] arguments, object result, string source, bool handled, long milliseconds, Exception ex);
    }
}