// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.Repository
{
    public interface IByteStoreEngine : IDataStoreEngine
    {
        Stream Get(string name);
        void Save(string name, Stream stream);
        bool Exists(string name);
    }
}
