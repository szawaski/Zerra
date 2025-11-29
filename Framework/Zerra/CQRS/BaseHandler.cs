// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Base class for command, event, and query handlers.
    /// Provides a standard implementation of the <see cref="IHandler"/> interface with context management.
    /// </summary>
    public abstract class BaseHandler : IHandler
    {
        private BusContext? context;
        
        /// <summary>
        /// Gets the bus context associated with this handler.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the handler has not been initialized.</exception>
        public BusContext Context => context ?? throw new InvalidOperationException("Handler not initialized");

        /// <summary>
        /// Initializes the handler with the specified bus context.
        /// </summary>
        /// <param name="context">The bus context to associate with this handler.</param>
        void IHandler.Initialize(BusContext context)
        {
            this.context = context;
        }
    }
}