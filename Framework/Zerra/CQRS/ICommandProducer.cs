// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandProducer
    {
        void RegisterCommandType(Type type);
        IEnumerable<Type> GetCommandTypes();
        Task DispatchAsync(ICommand command, string source);
        Task DispatchAsyncAwait(ICommand command, string source);
    }
}