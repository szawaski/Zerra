// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Base interface for query handlers.
    /// Implement this interface along with specific query handler methods to handle queries routed through the bus.
    /// </summary>
    public interface IQueryHandler : IHandler
    {
        
    }
}
