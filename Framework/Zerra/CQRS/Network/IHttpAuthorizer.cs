// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.CQRS.Network
{
    public interface IHttpAuthorizer
    {
        void Authorize(IDictionary<string, IList<string>> headers);
        IDictionary<string, IList<string>> BuildAuthHeaders();
    }
}