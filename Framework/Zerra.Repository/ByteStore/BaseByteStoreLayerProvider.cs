// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;
using System.Threading.Tasks;
using Zerra.Providers;

namespace Zerra.Repository
{
    public abstract class BaseByteStoreLayerProvider<TNextProviderInterface> : BaseLayerProvider<TNextProviderInterface>, IByteStoreProvider
        where TNextProviderInterface : IByteStoreProvider
    {
        public bool Exists(string name)
        {
            return NextProvider.Exists(name);
        }
        public Stream Get(string name)
        {
            var stream = NextProvider.Get(name);
            return OnGet(stream);
        }
        public void Save(string name, Stream stream)
        {
            var resultStream = OnSave(stream);
            NextProvider.Save(name, resultStream);
        }

        public Task<bool> ExistsAsync(string name)
        {
            return NextProvider.ExistsAsync(name);
        }
        public async Task<Stream> GetAsync(string name)
        {
            var stream = await NextProvider.GetAsync(name);
            return await OnGetAsync(stream);
        }
        public async Task SaveAsync(string name, Stream stream)
        {
            var resultStream = await OnSaveAsync(stream);
            await NextProvider.SaveAsync(name, resultStream);
        }

        protected virtual Stream OnGet(Stream stream) { return stream; }
        protected virtual Stream OnSave(Stream stream) { return stream; }

        protected virtual Task<Stream> OnGetAsync(Stream stream) { return Task.FromResult(stream); }
        protected virtual Task<Stream> OnSaveAsync(Stream stream) { return Task.FromResult(stream); }
    }
}
