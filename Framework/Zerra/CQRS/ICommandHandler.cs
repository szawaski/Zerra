// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// A handler method for a command.
    /// One practice is a collective interface for a set of these so an implementation can handle multiples and configuration is easy.
    /// </summary>
    /// <typeparam name="T">The command type</typeparam>
    public interface ICommandHandler<T> : IHandler where T : ICommand
    {
        /// <summary>
        /// Handles processing the command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> to await processing the command.</returns>
        Task Handle(T command, CancellationToken cancellationToken);
    }
}