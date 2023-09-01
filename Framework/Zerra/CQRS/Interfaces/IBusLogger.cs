// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        Task LogCommandAsync(Type commandType, ICommand command, string source, Exception ex);
        Task LogEventAsync(Type eventType, IEvent @event, string source, Exception ex);
        Task LogCallAsync(Type interfaceType, string methodName, object[] arguments, string source, Exception ex);
    }
}