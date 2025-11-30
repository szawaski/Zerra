// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.AzureServiceBus
{
    /// <summary>
    /// Represents a message envelope for Azure Service Bus CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Wraps command or event data with metadata including message type, result indication,
    /// security claims, and message source for routing and processing in Azure Service Bus-based CQRS systems.
    /// </remarks>
    public sealed class AzureServiceBusMessage
    {
        /// <summary>
        /// Gets or sets the serialized message data (command or event).
        /// </summary>
        public byte[]? MessageData { get; set; }

        /// <summary>
        /// Gets or sets the type of the message (command or event class).
        /// </summary>
        public Type? MessageType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message expects a result response.
        /// </summary>
        /// <remarks>
        /// True for commands with results or queries; false for fire-and-forget commands and events.
        /// </remarks>
        public bool HasResult { get; set; }

        /// <summary>
        /// Gets or sets the security claims associated with the message sender.
        /// </summary>
        /// <remarks>
        /// A jagged array where each inner array contains a claim type and value pair.
        /// Null if no claims are associated with the message.
        /// </remarks>
        public string[][]? Claims { get; set; }

        /// <summary>
        /// Gets or sets the originating service or source of the message.
        /// </summary>
        public string? Source { get; set; }
    }
}
