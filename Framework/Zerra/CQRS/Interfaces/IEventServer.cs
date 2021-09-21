// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IEventServer
    {
        string ConnectionString { get; }
        void RegisterEventType(Type type);
        ICollection<Type> GetEventTypes();
        void SetHandler(Func<IEvent, Task> handlerAsync);
        void Open();
        void Close();
    }
}