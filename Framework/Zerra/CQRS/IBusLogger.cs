// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        void BeginCommandAsync(Type commandType, ICommand command, string source, bool handled);
        void BeginEventAsync(Type eventType, IEvent @event, string source, bool handled);
        void BeginCallAsync(Type interfaceType, string methodName, object[] arguments, object? result, string source, bool handled);

        void EndCommandAsync(Type commandType, ICommand command, string source, bool handled, long milliseconds, Exception? ex);
        void EndEventAsync(Type eventType, IEvent @event, string source, bool handled, long milliseconds, Exception? ex);
        void EndCallAsync(Type interfaceType, string methodName, object[] arguments, object? result, string source, bool handled, long milliseconds, Exception? ex);
    }
}