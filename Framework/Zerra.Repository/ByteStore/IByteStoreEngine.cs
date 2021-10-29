// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;
using System.Threading.Tasks;

namespace Zerra.Repository
{
    public interface IByteStoreEngine : IDataStoreEngine
    {
        Stream Get(string name);
        void Save(string name, Stream stream);
        bool Exists(string name);

        Task<Stream> GetAsync(string name);
        Task SaveAsync(string name, Stream stream);
        Task<bool> ExistsAsync(string name);
    }
}
