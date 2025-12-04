using Zerra.Logging;

namespace Zerra.CQRS
{
    /// <summary>
    /// Base interface for command, event, and query handlers.
    /// Provides access to the bus context and initialization support.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Gets the bus instance associated with this handler for publishing and consuming messages.
        /// </summary>
        IBus Bus { get; }

        /// <summary>
        /// Gets the logger instance for recording diagnostic and operational events for this handler.
        /// May be <see langword="null"/> if logging is not configured.
        /// </summary>
        ILog? Log { get; }

        /// <summary>
        /// Gets the bus context containing scope and contextual information for the current operation.
        /// This context is populated during handler initialization and contains data such as tenant information,
        /// user context, and other scoped values needed for message processing.
        /// </summary>
        BusContext Context { get; }

        /// <summary>
        /// Initializes the handler with the specified bus context.
        /// This method is called by the bus before the handler processes any messages and ensures
        /// that the handler has access to all necessary context information.
        /// </summary>
        /// <param name="context">The bus context to associate with this handler.</param>
        void Initialize(BusContext context);
    }
}
