// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    public class ServiceSetting
    {
        public string Name { get; set; }
        public string InternalUrl { get; set; }
        public string ExternalUrl { get; set; }
        public string EncryptionKey { get; set; }
        public string[] Types { get; set; }
        public string[] InstantiateTypes { get; set; }
    }
}