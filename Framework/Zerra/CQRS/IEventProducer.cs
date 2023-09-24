// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IEventProducer
    {
        void RegisterEventType(Type type);
        IEnumerable<Type> GetEventTypes();
        Task DispatchAsync(IEvent @event, string source);
    }
}