// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// The trace context received with the message being handled, so tracing can continue a trace across services.
    /// </summary>
    /// <remarks>
    /// Every transport sends the caller's <see cref="System.Diagnostics.Activity.Current"/> id with a message, or <see cref="TraceParent"/> when there is no current activity,
    /// and sets <see cref="TraceParent"/> to the id it received before the handler runs. Without tracing, the id passes through a service unchanged.
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
    }
}
