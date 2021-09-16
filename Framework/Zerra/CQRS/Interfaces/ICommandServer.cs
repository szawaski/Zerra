// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandServer
    {
        string ServiceUrl { get; }
        void RegisterCommandType(Type type);
        ICollection<Type> GetCommandTypes();
        void SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync);
        void Open();
        void Close();
    }
}