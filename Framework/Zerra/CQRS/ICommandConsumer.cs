// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandConsumer
    {
        void RegisterCommandType(int maxConcurrent, string topic, Type type);
        void Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync);
        void Open();
        void Close();
    }

    public delegate Task HandleRemoteCommandDispatch(ICommand command, string source, bool isApi);
    public delegate Task<object?> HandleRemoteCommandWithResultDispatch(ICommand command, string source, bool isApi);
}