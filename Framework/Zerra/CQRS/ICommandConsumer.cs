// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines a command consumer that can receive and process commands
    /// </summary>
    public interface ICommandConsumer
    {
        /// <summary>
        /// The host information.
        /// </summary>
        string MessageHost { get; }
        /// <summary>
        /// Registers a command type for this interface.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this command consumer.</param>
        /// <param name="topic">The message service topic.</param>
        /// <param name="type">The command type.</param>
        void RegisterCommandType(int maxConcurrent, string topic, Type type);
        /// <summary>
        /// A method called from <see cref="Bus"/> on startup to provide parts needed for the server.
        /// </summary>
        /// <param name="commandCounter">A counter to track and limit requests.</param>
        /// <param name="handlerAsync">The hander delegate router that will link the acutal command methods.</param>
        /// <param name="handlerAwaitAsync">The hander delegate router that will link the acutal command async methods.</param>
        /// <param name="handlerWithResultAwaitAsync">The hander delegate router that will link the acutal command with result methods.</param>
        void Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync);
        /// <summary>
        /// A method called from <see cref="Bus"/> to start receiving.
        /// </summary>
        void Open();
        /// <summary>
        /// A method called from <see cref="Bus"/> to stop receiving.
        /// </summary>
        void Close();
    }

    /// <summary>
    /// A delegate that a command consumer will use to handle a received command.
    /// <see cref="Bus"/> will provide the delegate.
    /// </summary>
    public delegate Task HandleRemoteCommandDispatch(ICommand command, string source, bool isApi, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate that a command consumer will use to handle a received command with a result.
    /// <see cref="Bus"/> will provide the delegate.
    /// </summary>
    public delegate Task<object?> HandleRemoteCommandWithResultDispatch(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
}