// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    /// <summary>
    /// Service information for a command or event consumer.
    /// </summary>
    [Zerra.Reflection.GenerateTypeDetail]
    public sealed class ServiceMessageSetting
    {
        /// <summary>
        /// The name of the service.
        /// This should match a service name specified in <see cref="CQRSSettings.Get(string, bool)"/> or an entry assembly when using <see cref="CQRSSettings.Get(bool)"/>.
        /// </summary>
        public string? Service { get; internal set; }

        /// <summary>
        /// The message service connection information.
        /// Often this has secure information so for production it is not good practice to store this in the JSON file.
        /// Use <see cref="SetMessageHost(string)"/> or <see cref="ServiceSettings.SetAllMessageHosts(string)"/> to set the message host.
        /// </summary>
        public string? MessageHost { get; internal set; }
        /// <summary>
        /// The encryption key for the service.
        /// For production it is not good practice to store the key in the JSON file.
        /// Use <see cref="SetEncryptionKey"/> or <see cref="ServiceSettings.SetAllEncryptionKeys(string)"/> to set the encryption key.
        /// </summary>
        public string? EncryptionKey { get; internal set; }
        /// <summary>
        /// The command and event handler interface types that the service will host.
        /// The simple interface names works, full qualified names are not required.
        /// Using the <see cref="ServiceExposedAttribute"/> on commands and events is also required.
        /// </summary>
        public string[]? Types { get; internal set; }

        /// <summary>
        /// Set the encryption key for the service.
        /// </summary>
        /// <param name="encryptionKey">The secure encryption key.</param>
        public void SetEncryptionKey(string encryptionKey)
        {
            this.EncryptionKey = encryptionKey;
        }

        /// <summary>
        /// Set the message host for the service.
        /// </summary>
        /// <param name="messageHost">The message host.</param>
        public void SetMessageHost(string messageHost)
        {
            this.MessageHost = messageHost;
        }
    }
}