// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Linq;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Settings
{
    /// <summary>
    /// Settings for each service deserialized from cqrssettings.json or cqrssettings.{Environment}.json.
    /// </summary>
    [Zerra.Reflection.GenerateTypeDetail]
    public sealed class ServiceSettings
    {
        /// <summary>
        /// The name of the running service set by <see cref="CQRSSettings.Get(bool)"/>.
        /// This is not apart of the values in cqrssettings.json or cqrssettings.{Environment}.json.
        /// </summary>
        [JsonIgnore]
        public string? ThisServiceName { get; internal set; }

        /// <summary>
        /// The service information for query servers.
        /// </summary>
        public ServiceQuerySetting[]? Queries { get; internal set; }

        /// <summary>
        /// The service information for command and event consumers.
        /// </summary>
        public ServiceMessageSetting[]? Messages { get; internal set; }

        /// <summary>
        /// Set the encryption key for all the services.
        /// </summary>
        /// <param name="encryptionKey">The secure encryption key.</param>
        public void SetAllEncryptionKeys(string encryptionKey)
        {
            this.Queries?.ForEach(x => x.SetEncryptionKey(encryptionKey));
            this.Messages?.ForEach(x => x.SetEncryptionKey(encryptionKey));
        }

        /// <summary>
        /// Sets the message host for all the services.
        /// </summary>
        /// <param name="messageHost">The connection information for the message host.</param>
        public void SetAllMessageHosts(string messageHost)
        {
            this.Messages?.ForEach(x => x.SetMessageHost(messageHost));
        }
    }
}