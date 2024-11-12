// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// A handler method for a command that returns a result.
    /// One practice is a collective interface for a set of these so an implementation can handle multiples and configuration is easy.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICommandHandler<T, TResult> where T : ICommand<TResult>
    {
        Task<TResult> Handle(T command);
    }
}