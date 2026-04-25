// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// A handler method for a command that returns a result.
    /// One practice is a collective interface for a set of these so an implementation can handle multiples and configuration is easy.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICommandHandler<T, TResult> : IHandler where T : ICommand<TResult>
    {
        /// <summary>
        /// Handles processing the command and returns a result.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> to await processing the command and retreiving the result.</returns>
        Task<TResult> Handle(T command, CancellationToken cancellationToken);
    }
}