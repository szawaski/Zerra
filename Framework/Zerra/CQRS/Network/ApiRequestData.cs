// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Represents the data structure for an API request, including provider and message details.
    /// </summary>
    [GenerateTypeDetail]
    public sealed class ApiRequestData
    {
        /// <summary>Gets or sets the fully qualified type name of the provider to invoke.</summary>
        public string? ProviderType { get; set; }
        /// <summary>Gets or sets the method name to invoke on the provider.</summary>
        public string? ProviderMethod { get; set; }
        /// <summary>Gets or sets the serialized arguments for the provider method.</summary>
        public byte[]?[]? ProviderArguments { get; set; }

        /// <summary>Gets or sets the fully qualified type name of the message.</summary>
        public string? MessageType { get; set; }
        /// <summary>Gets or sets the serialized message data.</summary>
        public string? MessageData { get; set; }
        /// <summary>Gets or sets a value indicating whether to await the message processing.</summary>
        public bool MessageAwait { get; set; }
        /// <summary>Gets or sets a value indicating whether a result is expected from message processing.</summary>
        public bool MessageResult { get; set; }

        /// <summary>Gets or sets the source identifier of the request.</summary>
        public string? Source { get; set; }
    }
}