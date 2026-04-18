// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Repository
{
    public sealed class ByteStoreProvider<TContext> : IByteStoreProvider
        where TContext : DataContext, new()
    {
        private readonly IByteStoreEngine Engine;

        public ByteStoreProvider()
        {
            var context = new TContext();
            if (!context.TryGetEngine(out var engine))
                throw new Exception($"{typeof(TContext).Name} could not produce an engine of {typeof(IByteStoreEngine).Name}");
            if (engine is not IByteStoreEngine byteStoreEngine)
                throw new Exception($"{typeof(TContext).Name} produced an engine of {engine.GetType().Name} which is not a {typeof(IByteStoreEngine).Name}");
            this.Engine = byteStoreEngine;
        }

        public bool Exists(string name)
        {
            return Engine.Exists(name);
        }
        public Stream Get(string name)
        {
            return Engine.Get(name);
        }
        public void Save(string name, Stream stream)
        {
            Engine.Save(name, stream);
        }

        public Task<bool> ExistsAsync(string name)
        {
            return Engine.ExistsAsync(name);
        }
        public Task<Stream> GetAsync(string name)
        {
            return Engine.GetAsync(name);
        }
        public Task SaveAsync(string name, Stream stream)
        {
            return Engine.SaveAsync(name, stream);
        }
    }
}