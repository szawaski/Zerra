namespace Zerra.CQRS
{
    /// <summary>
    /// Base interface for command, event, and query handlers.
    /// Provides access to the bus context and initialization support.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Gets the bus context associated with this handler.
        /// </summary>
        BusContext Context { get; }
        /// <summary>
        /// Initializes the handler with the specified bus context.
        /// </summary>
        /// <param name="context">The bus context to associate with this handler.</param>
        void Initialize(BusContext context);
    }
}
