﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Settings
{
    public sealed class ServiceQuerySetting
    {
        public string? Service { get; internal set; }

        public string? BindingUrl { get; internal set; }
        public string? ExternalUrl { get; internal set; }

        public string? EncryptionKey { get; internal set; }

        public string[]? Types { get; internal set; }

        public void SetEncryptionKey(string encryptionKey)
        {
            this.EncryptionKey = encryptionKey;
        }
    }
}