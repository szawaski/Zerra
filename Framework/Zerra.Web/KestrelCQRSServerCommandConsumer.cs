// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.CQRS;

namespace Zerra.Web
{
    public sealed class KestrelCQRSServerCommandConsumer : ICommandConsumer
    {
        private readonly KestrelCQRSServerLinkedSettings settings;
        public KestrelCQRSServerCommandConsumer(KestrelCQRSServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        public string ConnectionString => throw new NotImplementedException();

        public void Close() { }

        public IEnumerable<Type> GetCommandTypes()
        {
            return settings.CommandTypes;
        }

        public void Open() { }

        public void RegisterCommandType(Type type)
        {
            settings.CommandTypes.Add(type);
        }

        public void SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
        {
            settings.HandlerAsync = handlerAsync;
            settings.HandlerAwaitAsync = handlerAwaitAsync;
        }
    }
}