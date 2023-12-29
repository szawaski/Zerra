// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;

namespace Zerra.Identity.Jwt
{
    public sealed class JwtKey
    {
        [JsonProperty(PropertyName = "kty")]
        public string KeyType { get; set; }

        [JsonProperty(PropertyName = "use")]
        public string Use { get; set; }

        [JsonProperty(PropertyName = "kid")]
        public string KeyID { get; set; }

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
        //The same (in use and value) as kid. However, this is a legacy claim emitted only in v1.0 id_tokens for compatibility purposes.
        [JsonProperty(PropertyName = "x5t")]
        public string X509Thumbprint { get; set; }

        [JsonProperty(PropertyName = "n")]
        public string Modulus { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Exponent { get; set; }

        [JsonProperty(PropertyName = "x5c")]
        public string[] X509Certificates { get; set; }
    }
}
