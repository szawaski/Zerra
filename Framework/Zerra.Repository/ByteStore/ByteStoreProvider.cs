// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

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

        public bool Exists(string source, string name)
        {
            throw new NotImplementedException();
        }

        public Stream Get(string source, string name)
        {
            throw new NotImplementedException();
        }

        public void Save(string source, string name, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}