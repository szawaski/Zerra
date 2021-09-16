// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;
using System.Security.Claims;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public class CQRSRequestData
    {
        public string ProviderType { get; set; }
        public string ProviderMethod { get; set; }
        public string[] ProviderArguments { get; set; }

        public string MessageType { get; set; }
        public string MessageData { get; set; }
        public bool MessageAwait { get; set; }

        public CallDataClaim[] Claims { get; set; }

        public class CallDataClaim
        {
            public string Type { get; set; }
            public string Value { get; set; }
        }

        public void AddProviderArguments(object[] arguments)
        {
            this.ProviderArguments = arguments.Select(x => JsonSerializer.Serialize(x)).ToArray();
        }
        public void AddClaims()
        {
            if (System.Threading.Thread.CurrentPrincipal is ClaimsPrincipal principal)
            {
                if (principal.Identity is ClaimsIdentity identity)
                {
                    this.Claims = identity.Claims.Select(x => new CallDataClaim() { Type = x.Type, Value = x.Value }).ToArray();
                }
            }
        }
    }
}