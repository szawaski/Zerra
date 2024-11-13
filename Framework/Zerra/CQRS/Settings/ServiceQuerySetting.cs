// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    /// <summary>
    /// Service information for a query server.
    /// </summary>
    [Zerra.Reflection.GenerateTypeDetail]
    public sealed class ServiceQuerySetting
    {
        /// <summary>
        /// The name of the service.
        /// This should match a service name specified in <see cref="CQRSSettings.Get(string, bool)"/> or an entry assembly when using <see cref="CQRSSettings.Get(bool)"/>.
        /// </summary>
        public string? Service { get; internal set; }

        /// <summary>
        /// The binding url of this service.
        /// If left blank <see cref="CQRSSettings"/> can search for standard variables in the configuration and environmental arguments.
        /// </summary>
        public string? BindingUrl { get; internal set; }
        /// <summary>
        /// The external url of this service so other services can find it.
        /// If not supplied <see cref="CQRSSettings"/> will search by the Service Name for the url in the configuration and environmental arguments.
        /// </summary>
        public string? ExternalUrl { get; internal set; }

        /// <summary>
        /// The encryption key for the service.
        /// For production it is not good practice to store the key in the JSON file.
        /// Use <see cref="SetEncryptionKey"/> or <see cref="ServiceSettings.SetAllEncryptionKeys(string)"/> to set the encryption key.
        /// </summary>
        public string? EncryptionKey { get; internal set; }

        /// <summary>
        /// The query interface types that the service will host.
        /// The simple interface names works, full qualified names are not required.
        /// Using the <see cref="ServiceExposedAttribute"/> on query interfaces is also required.
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
    }
}