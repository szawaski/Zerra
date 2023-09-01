// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandConsumer
    {
        string ServiceUrl { get; }
        void RegisterCommandType(Type type);
        IEnumerable<Type> GetCommandTypes();
        void SetHandler(CommandHandlerDelegate handlerAsync, CommandHandlerDelegate handlerAwaitAsync);
        void Open();
        void Close();
    }

    public delegate Task CommandHandlerDelegate(ICommand command, string source);
}