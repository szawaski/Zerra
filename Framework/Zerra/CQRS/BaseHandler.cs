// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.CQRS
{
    /// <summary>
    /// Base class for command, event, and query handlers.
    /// Provides a standard implementation of the <see cref="IHandler"/> interface with context management.
    /// </summary>
    public abstract class BaseHandler : IHandler
    {
        /// <inheritdoc />
        public IBus Bus => Context.Bus;

        /// <inheritdoc />
        public ILog? Log => Context.Log;

        private BusContext? context = null;
        public BusContext Context => context ?? throw new InvalidOperationException("Handler not initialized");

        /// <inheritdoc />
        void IHandler.Initialize(BusContext context)
        {
            this.context = context;
        }
    }
}