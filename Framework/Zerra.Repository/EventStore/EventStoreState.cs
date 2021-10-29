// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public enum EventStoreState : byte
    {
        Any = 0,
        NotExisting = 1,
        Existing = 2
    }
}
