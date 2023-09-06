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
        void SetHandler(HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync);
        void Open();
        void Close();
    }

    public delegate Task HandleRemoteCommandDispatch(ICommand command, string source, bool isApi);
}