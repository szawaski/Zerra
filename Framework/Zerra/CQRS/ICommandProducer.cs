// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines a command producer to send commands.
    /// </summary>
    public interface ICommandProducer
    {
        /// <summary>
        /// Registers a command for this producer.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this command producer.</param>
        /// <param name="topic">The message service topic.</param>
        /// <param name="type">The command type.</param>
        void RegisterCommandType(int maxConcurrent, string topic, Type type);
        /// <summary>
        /// Executes sending a command.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="source">A description of where the command came from.</param>
        /// <returns>A task to await sending the command.</returns>
        Task DispatchAsync(ICommand command, string source);
        /// <summary>
        /// Executes sending a command and await a response when the command has processed.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="source">A description of where the command came from.</param>
        /// <returns>A task to await sending and processing of the command.</returns>
        Task DispatchAwaitAsync(ICommand command, string source);
        /// <summary>
        /// Executes sending a command and returns a result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="command">The command to send.</param>
        /// <param name="source">A description of where the command came from.</param>
        /// <returns>A task to await the result of the command.</returns>
        Task<TResult?> DispatchAwaitAsync<TResult>(ICommand<TResult> command, string source);
    }
}