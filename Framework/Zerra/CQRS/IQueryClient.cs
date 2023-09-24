// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    public interface IQueryClient
    {
        TReturn Call<TReturn>(Type interfaceType, string methodName, object[] arguments, string source);
    }
}