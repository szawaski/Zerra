// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;

namespace Zerra.CQRS.Settings
{
    public class ServiceSettings
    {
        public string ThisServiceName { get; internal set; }

        public string MessageHost { get; internal set; }
        public string RelayUrl { get; internal set; }
        public string RelayKey { get; internal set; }
        public ServiceSetting[] Services { get; internal set; }

        public void SetMessageHost(string messageHost)
        {
            this.MessageHost = messageHost;
        }

        public void SetRelayKey(string relayKey)
        {
            this.RelayKey = relayKey;
        }

        public void SetServiceEncryptionKey(string serviceName, string encryptionKey)
        {
            var service = this.Services.FirstOrDefault(x => x.Name == serviceName);
        }
    }
}