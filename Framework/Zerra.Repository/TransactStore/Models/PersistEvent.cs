// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public sealed class PersistEvent
    {
        public Guid ID { get; }
        public string Name { get; }
        public object? Source { get; }
        public PersistEvent(Guid id, string name, object? source)
        {
            this.ID = id;
            this.Name = name;
            this.Source = source;
        }
    }
}
