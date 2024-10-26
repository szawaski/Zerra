// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Json
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonIgnoreAttribute : Attribute
    {
        public JsonIgnoreCondition Condition { get; }
        public JsonIgnoreAttribute()
        {
            this.Condition = JsonIgnoreCondition.Always;
        }
        public JsonIgnoreAttribute(JsonIgnoreCondition condition)
        {
            this.Condition = condition;
        }
    }
}