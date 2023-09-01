// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        ValueTask LogCommandAsync(Type commandType, ICommand command, string source);
        ValueTask LogEventAsync(Type eventType, IEvent @event, string source);
        ValueTask LogCallAsync(Type interfaceType, string methodName, object[] arguments, string source);
    }
}