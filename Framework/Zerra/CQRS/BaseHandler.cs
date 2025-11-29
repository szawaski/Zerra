// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public abstract class BaseHandler : IHandler
    {
        private BusContext? context;
        public BusContext Context => context ?? throw new InvalidOperationException("Handler not initialized");

        void IHandler.Initialize(BusContext context)
        {
            this.context = context;
        }
    }
}