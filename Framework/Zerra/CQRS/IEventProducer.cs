// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IEventProducer
    {
        string ConnectionString { get; }
        Task DispatchAsync(IEvent @event, string source);
    }
}