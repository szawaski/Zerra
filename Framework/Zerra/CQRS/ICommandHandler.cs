// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// A handler method for a command.
    /// One practice is a collective interface for a set of these so an implementation can handle multiples and configuration is easy.
    /// </summary>
    /// <typeparam name="T">The command type</typeparam>
    public interface ICommandHandler<T> where T : ICommand
    {
        Task Handle(T command);
    }
}