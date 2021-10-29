// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public class PersistEvent
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public object Source { get; set; }
    }
}
