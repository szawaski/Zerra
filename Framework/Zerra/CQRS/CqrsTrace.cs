// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;

namespace Zerra.CQRS
{
    /// <summary>
    /// The trace context received with the message being handled, so tracing can continue a trace across services.
    /// </summary>
    /// <remarks>
    /// Every transport sends <see cref="GetTraceParentToSend"/> with a message, and sets <see cref="TraceParent"/> to the id it received before the handler runs.
    /// Without tracing, the id passes through a service unchanged.
    /// </remarks>
    public static class CqrsTrace
    {
        private static readonly AsyncLocal<string?> traceParent = new();

        /// <summary>
        /// The W3C trace parent id received with the message being handled, or null if the message had none or wasn't received from another service.
        /// </summary>
        public static string? TraceParent
        {
            get => traceParent.Value;
            set => traceParent.Value = value;
        }

        /// <summary>
        /// The W3C trace parent id to send with an outgoing message: the current <see cref="Activity"/> while it's running, otherwise the <see cref="TraceParent"/> received.
        /// </summary>
        /// <returns>The trace parent id, or null when there is no trace.</returns>
        public static string? GetTraceParentToSend()
        {
            //a finished activity can still be current where it was captured, such as a server's receive loop started during startup, it isn't this message's trace
            var current = Activity.Current;
            if (current is not null && !current.IsStopped)
                return current.Id;
            return traceParent.Value;
        }
    }
}
