// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    public enum ContentType : byte
    {
        Bytes = 0,
        Json = 1,
        JsonNameless = 2
    }
}