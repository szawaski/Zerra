// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Linq;

namespace Zerra.CQRS.Settings
{
    [Zerra.Reflection.GenerateTypeDetail]
    public sealed class ServiceSettings
    {
        public string? ThisServiceName { get; internal set; }

        public ServiceQuerySetting[]? Queries { get; internal set; }

        public ServiceMessageSetting[]? Messages { get; internal set; }

        public void SetAllEncryptionKeys(string encryptionKey)
        {
            this.Queries?.ForEach(x => x.SetEncryptionKey(encryptionKey));
            this.Messages?.ForEach(x => x.SetEncryptionKey(encryptionKey));
        }

        public void SetAllMessageHosts(string messageHost)
        {
            this.Messages?.ForEach(x => x.SetMessageHost(messageHost));
        }
    }
}