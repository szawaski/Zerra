// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public enum BusLogging : byte
    {
        Logged = 0,
        HandlerOnly = 1,
        None = 2
    }
}
