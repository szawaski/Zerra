// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;

namespace Zerra.Identity.Jwt
{
    public class JwtOpenIDPayload
    {
        //Standard---------------------------------------

        [JsonProperty(PropertyName = "iss")]
        public string Issuer { get; set; }

        [JsonProperty(PropertyName = "sub")]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = "aud")]
        public string Audience { get; set; }

        [JsonProperty(PropertyName = "exp")]
        public long Expiration { get; set; }

        [JsonProperty(PropertyName = "iat")]
        public long IssuedAtTime { get; set; }

        [JsonProperty(PropertyName = "nbf")]
        public long NotBefore { get; set; }

        [JsonProperty(PropertyName = "jti")]
        public string JsonTokenID { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "prn")]
        public string Principal { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        [JsonProperty(PropertyName = "unique_name")]
        public string UniqueName { get; set; }

        //Azure AD---------------------------------------

        [JsonProperty(PropertyName = "tid")]
        public string AAD_TenantID { get; set; }

        [JsonProperty(PropertyName = "ver")]
        public string AAD_Version { get; set; }

        [JsonProperty(PropertyName = "oid")]
        public string AAD_ObjectID { get; set; }

        [JsonProperty(PropertyName = "upn")]
        public string AAD_UserPrincipalName { get; set; }

        [JsonProperty(PropertyName = "given_name")]
        public string AAD_UserFirstName { get; set; }

        [JsonProperty(PropertyName = "family_name")]
        public string AAD_UserLastName { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string AAD_Name { get; set; }

        //Azure AD B2C--------------------------------------------

        [JsonProperty(PropertyName = "auth_time")]
        public long AAD_AuthTime { get; set; }

        [JsonProperty(PropertyName = "emails")]
        public string[] AAD_Emails { get; set; }

        [JsonProperty(PropertyName = "tfp")]
        public string AAD_UserFlowPolicy { get; set; }

        //Custom For This Assembly--------------------------------------------

        [JsonProperty(PropertyName = "roles")]
        public string[] Roles { get; set; }


        //Unidentified Claims Seen---------------------------------------

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
        //An internal claim used by Azure AD to record data for token reuse. Should be ignored.
        //[JsonProperty(PropertyName = "aio")]
        //public string aio { get; set; }

        //[JsonProperty(PropertyName = "amr")]
        //public string[] amr { get; set; }

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
        //The code hash is included in ID tokens only when the ID token is issued with an OAuth 2.0 authorization code. It can be used to validate the authenticity of an authorization code. For details about performing this validation, see the OpenID Connect specification.
        //[JsonProperty(PropertyName = "c_hash")]
        //public string c_hash { get; set; }

        //[JsonProperty(PropertyName = "ipaddr")]
        //public string ipaddr { get; set; }

        //[JsonProperty(PropertyName = "onprem_sid")]
        //public string onprem_sid { get; set; }

        //https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
        //An internal claim used by Azure to revalidate tokens. Should be ignored.
        //[JsonProperty(PropertyName = "uti")]
        //public string uti { get; set; }
    }
}
