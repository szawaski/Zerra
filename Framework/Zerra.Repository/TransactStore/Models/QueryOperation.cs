// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public enum QueryOperation : byte
    {
        Many,
        First,
        Single,
        Any,
        Count,

        EventMany,
        EventFirst,
        EventSingle,
        EventAny,
        EventCount
    }
}
