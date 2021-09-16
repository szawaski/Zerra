// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IEventClient
    {
        string ServiceUrl { get; }
        Task DispatchAsync(IEvent @event);
    }
}