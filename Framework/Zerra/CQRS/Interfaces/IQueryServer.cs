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
        string ServiceUrl { get; }
        void RegisterInterfaceType(Type type);
        ICollection<Type> GetInterfaceTypes();
        void SetHandler(QueryHandlerDelegate providerHandlerAsync);
        void Open();
        void Close();
    }

    public delegate Task<RemoteQueryCallResponse> QueryHandlerDelegate(Type interfaceName, string methodName, string[] arguments, string source, bool isApi);
}