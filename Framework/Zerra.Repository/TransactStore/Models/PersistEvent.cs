// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public sealed class PersistEvent
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public object? Source { get; private set; }
        public PersistEvent(Guid id, string name, object? source)
        {
            this.ID = id;
            this.Name = name;
            this.Source = source;
        }
    }
}
