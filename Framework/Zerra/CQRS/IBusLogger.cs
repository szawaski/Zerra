// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        Task LogCommandAsync(Type commandType, ICommand command, string source, bool handled, long milliseconds, Exception ex);
        Task LogEventAsync(Type eventType, IEvent @event, string source, bool handled, long milliseconds, Exception ex);
        Task LogCallAsync(Type interfaceType, string methodName, object[] arguments, object result, string source, bool handled, long milliseconds, Exception ex);
    }
}