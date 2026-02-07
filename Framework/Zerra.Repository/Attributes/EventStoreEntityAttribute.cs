// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EventStoreEntityAttribute<T> : BaseGenerateAttribute
    {
        private readonly bool? eventLinking;
        private readonly bool? queryLinking;
        private readonly bool? persistLinking;

        public EventStoreEntityAttribute()
        {
            this.eventLinking = null;
            this.queryLinking = null;
            this.persistLinking = null;
        }

        public EventStoreEntityAttribute(bool linking)
        {
            this.eventLinking = linking;
            this.queryLinking = linking;
            this.persistLinking = linking;
        }

        public EventStoreEntityAttribute(bool eventLinking, bool queryLinking, bool persistLinking)
        {
            this.eventLinking = eventLinking;
            this.queryLinking = queryLinking;
            this.persistLinking = persistLinking;
        }

        public override Type? Generate(Type type) => RepositoryProviderGenerator.GenerateEventStoreAsTransactStoreProvider<T>(type, eventLinking == true, queryLinking == true, persistLinking == true);
    }
}
