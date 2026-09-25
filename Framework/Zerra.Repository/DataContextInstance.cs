// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// The context a store provider uses when it isn't given one: one instance per context type, so every provider on the type reaches the
    /// same engine. For a database that changes nothing, but an in-memory engine is its own store, and models related to each other have to
    /// be in the same one.
    /// </summary>
    internal static class DataContextInstance<TContext>
        where TContext : DataContext, new()
    {
        public static readonly TContext Shared = new();
    }
}
