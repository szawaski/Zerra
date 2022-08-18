// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    public class ServiceSettings
    {
        public string ThisServiceName { get; set; }

        public string MessageHost { get; set; }
        public string RelayUrl { get; set; }
        public string RelayKey { get; set; }
        public ServiceSetting[] Services { get; set; }
    }
}