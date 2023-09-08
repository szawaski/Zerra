// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandProducer
    {
        string ConnectionString { get; }
        Task DispatchAsync(ICommand command, string source);
        Task DispatchAsyncAwait(ICommand command, string source);
    }
}