﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Json
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyNameOldAttribute : Attribute
    {
        public string Name { get; }

        public JsonPropertyNameOldAttribute(string name)
        {
            Name = name;
        }
    }
}