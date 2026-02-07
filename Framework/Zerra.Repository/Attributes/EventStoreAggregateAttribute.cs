// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EventStoreAggregateAttribute<T> : BaseGenerateAttribute
    {
        public override Type? Generate(Type type) => RepositoryProviderGenerator.GenerateEventStoreProvider<T>(type);
    }
}
