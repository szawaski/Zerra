// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO;

namespace Zerra.Repository
{
    public interface IByteStoreProvider
    {
        Stream Get(string source, string name);
        void Save(string source, string name, Stream stream);
        bool Exists(string source, string name);
    }
}
