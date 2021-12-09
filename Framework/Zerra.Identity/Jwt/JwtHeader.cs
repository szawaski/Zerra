// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;

namespace Zerra.Identity.Jwt
{
    public class JwtHeader
    {
        [JsonProperty(PropertyName = "typ")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "alg")]
        public string Algorithm { get; set; }

        //[JsonProperty(PropertyName = "x5u")]
        //public string X509Url { get; set; }

        [JsonProperty(PropertyName = "kid")]
        public string KeyID { get; set; }

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
        //The same (in use and value) as kid. However, this is a legacy claim emitted only in v1.0 id_tokens for compatibility purposes.
        [JsonProperty(PropertyName = "x5t")]
        public string X509Thumbprint { get; set; }
    }
}
