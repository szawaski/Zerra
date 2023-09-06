﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
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

        public string ServiceUrl => throw new NotImplementedException();

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

        public void SetHandler(HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            settings.HandlerAsync = handlerAsync;
            settings.HandlerAwaitAsync = handlerAwaitAsync;
        }
    }
}