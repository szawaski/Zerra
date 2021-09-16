// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IMessageLogger
    {
        Task SaveAsync(Type messageType, IMessage message);
    }
}