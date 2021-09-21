// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IQueryServer
    {
        string ConnectionString { get; }
        void RegisterInterfaceType(Type type);
        ICollection<Type> GetInterfaceTypes();
        void SetHandler(Func<Type, string, string[], Task<RemoteQueryCallResponse>> providerHandlerAsync);
        void Open();
        void Close();
    }
}