// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public interface ICqrsAuthorizer
    {
        void Authorize(Dictionary<string, List<string?>> headers);
        ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync();
    }
}