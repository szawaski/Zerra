// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    public interface IHttpApiAuthorizer
    {
        void Authorize(HttpRequestHeader header);
        HttpAuthHeaders BuildAuthHeaders();
    }
}