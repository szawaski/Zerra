// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    public sealed class ServiceSetting
    {
        public string Name { get; internal set; }
        public string InternalUrl { get; internal set; }
        public string ExternalUrl { get; internal set; }
        public string EncryptionKey { get; internal set; }
        public string[] Types { get; internal set; }
        public string[] InstantiateTypes { get; internal set; }
    }
}