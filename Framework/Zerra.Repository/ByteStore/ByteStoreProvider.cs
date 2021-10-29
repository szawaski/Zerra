// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;
using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public class ByteStoreProvider<TContext> : IBaseProvider, IByteStoreProvider
        where TContext : DataContext<IByteStoreEngine>
    {
        protected readonly IByteStoreEngine Engine;

        public ByteStoreProvider()
        {
            var context = Instantiator.GetSingleInstance<TContext>();
            this.Engine = context.InitializeEngine();
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