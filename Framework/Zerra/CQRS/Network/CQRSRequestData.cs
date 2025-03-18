// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// The information needed sending a request to a remote CQRS service for a query, command, or event.
    /// </summary>
    public sealed class CqrsRequestData
    {
        /// <summary>
        /// The query provider interface type
        /// </summary>
        public string? ProviderType { get; set; }
        /// <summary>
        /// The query method called on the interface type.
        /// </summary>
        public string? ProviderMethod { get; set; }
        /// <summary>
        /// The query arguments for the method called.
        /// Serialized each using JSON.
        /// </summary>
        public string?[]? ProviderArguments { get; set; }

        /// <summary>
        /// The command or event type.
        /// </summary>
        public string? MessageType { get; set; }
        /// <summary>
        /// The command or event serialized based on the content type.
        /// </summary>
        public byte[]? MessageData { get; set; }
        /// <summary>
        /// The command is will wait for a response from the remote service when completed.
        /// </summary>
        public bool MessageAwait { get; set; }
        /// <summary>
        /// The command will wait for a result from the remote service when completed
        /// </summary>
        public bool MessageResult { get; set; }

        /// <summary>
        /// The raw claims in on the thread security principal.
        /// Formatted in string array pairs [name, value]
        /// </summary>
        public string[][]? Claims { get; set; }
        /// <summary>
        /// The description of the request source used for logging.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Helper to serialize query arguments for the method called.
        /// </summary>
        /// <param name="arguments">The raw argument values.</param>
        public void AddProviderArguments(object[] arguments)
        {
            this.ProviderArguments = arguments.Select(x => JsonSerializer.Serialize(x)).ToArray();
        }
    }
}