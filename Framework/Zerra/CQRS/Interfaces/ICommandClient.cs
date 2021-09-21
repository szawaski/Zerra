// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandClient
    {
        string ConnectionString { get; }
        Task DispatchAsync(ICommand command);
        Task DispatchAsyncAwait(ICommand command);
    }
}