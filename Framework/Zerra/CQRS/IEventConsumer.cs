// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IEventConsumer
    {
        string ServiceUrl { get; }
        void RegisterEventType(Type type);
        IEnumerable<Type> GetEventTypes();
        void SetHandler(HandleRemoteEventDispatch handlerAsync);
        void Open();
        void Close();
    }

    public delegate Task HandleRemoteEventDispatch(IEvent @event, string source, bool isApi);
}