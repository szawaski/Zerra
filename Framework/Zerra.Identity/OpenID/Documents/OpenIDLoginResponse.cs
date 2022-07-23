// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Zerra.Identity.TokenManagers;
using System.Linq;

namespace Zerra.Identity.OpenID.Documents
{
    public class OpenIDLoginResponse : OpenIDDocument
    {
        public string AccessCode { get; protected set; }

        public string ID { get; protected set; }
        public string Issuer { get; protected set; }
        public string Subject { get; protected set; }
        public string Audience { get; protected set; }
        public string UserID { get; protected set; }
        public string UserName { get; protected set; }
        public string Name { get; protected set; }
        public string[] Roles { get; protected set; }
        public string[] Emails { get; protected set; }
        public string X509Thumbprint { get; protected set; }
        public string KeyID { get; protected set; }

        public string Nonce { get; protected set; }
        public long? IssuedAtTime { get; protected set; }
        public long? NotBefore { get; protected set; }
        public long? Expiration { get; protected set; }

        public string State { get; set; }

        public Dictionary<string, string> OtherClaims { get; set; }

        public string Error { get; protected set; }
        public string ErrorDescription { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OpenIDLoginResponse(OpenIDResponseType responseType, string id, string issuer, string audience, IdentityModel identity, string x509Thumbprint, string nonce, string state)
        {
            if (responseType == OpenIDResponseType.Code)
            {
                this.AccessCode = AuthTokenManager.GenerateAccessCode(id, identity);
            }
            else if (responseType == OpenIDResponseType.IdToken)
            {
                this.ID = id;
                this.Issuer = issuer;
                this.Subject = Guid.NewGuid().ToString();
                this.Audience = audience;
                this.UserID = identity.UserID;
                this.UserName = identity.UserName;
                this.Roles = identity.Roles;

                this.KeyID = x509Thumbprint;
                this.X509Thumbprint = x509Thumbprint; //same https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens
                this.Nonce = nonce;
                this.State = state;

                this.IssuedAtTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                this.NotBefore = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
                this.Expiration = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
            }
            else
            {
                throw new IdentityProviderException($"Not supported response type {responseType}");
            }
        }

        public OpenIDLoginResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.AccessCode = json["code"]?.ToObject<string>();

            this.ID = json[nameof(JwtOpenIDPayload.JsonTokenID)]?.ToObject<string>();
            this.Issuer = json[nameof(JwtOpenIDPayload.Issuer)]?.ToObject<string>();
            this.Subject = json[nameof(JwtOpenIDPayload.Subject)]?.ToObject<string>();
            this.Audience = json[nameof(JwtOpenIDPayload.Audience)]?.ToObject<string>();
            this.UserID = json[nameof(JwtOpenIDPayload.AAD_ObjectID)]?.ToObject<string>();

            this.UserName = json[nameof(JwtOpenIDPayload.UniqueName)]?.ToObject<string>();
            if (this.UserName == null)
                this.UserName = json[nameof(JwtOpenIDPayload.AAD_UserPrincipalName)]?.ToObject<string>();

            this.Name = json[nameof(JwtOpenIDPayload.AAD_Name)]?.ToObject<string>();
            if (json[nameof(JwtOpenIDPayload.Roles)] != null)
            {
                if (json[nameof(JwtOpenIDPayload.Roles)].Type == JTokenType.Array)
                    this.Roles = json[nameof(JwtOpenIDPayload.Roles)]?.ToObject<string[]>();
                else
                    this.Roles = json[nameof(JwtOpenIDPayload.Roles)]?.ToObject<string>().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            }
            this.Emails = json[nameof(JwtOpenIDPayload.AAD_Emails)]?.ToObject<string[]>();

            this.Nonce = json[nameof(JwtOpenIDPayload.Nonce)]?.ToObject<string>();
            this.IssuedAtTime = json[nameof(JwtOpenIDPayload.IssuedAtTime)]?.ToObject<long>();
            this.NotBefore = json[nameof(JwtOpenIDPayload.NotBefore)]?.ToObject<long>();
            this.Expiration = json[nameof(JwtOpenIDPayload.Expiration)]?.ToObject<long>();

            this.X509Thumbprint = json[nameof(JwtHeader.X509Thumbprint)]?.ToObject<string>();
            this.KeyID = json[nameof(JwtHeader.KeyID)]?.ToObject<string>();

            this.State = json["state"]?.ToObject<string>();

            this.OtherClaims = OpenIDJwtBinding.GetOtherClaims(json);

            this.Error = json["error"]?.ToObject<string>();
            this.ErrorDescription = json["error_description"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.AccessCode != null)
                json.Add("code", JToken.FromObject(this.AccessCode));

            if (this.ID != null)
                json.Add(nameof(JwtOpenIDPayload.JsonTokenID), JToken.FromObject(this.ID));
            if (this.Issuer != null)
                json.Add(nameof(JwtOpenIDPayload.Issuer), JToken.FromObject(this.Issuer));
            if (this.Subject != null)
                json.Add(nameof(JwtOpenIDPayload.Subject), JToken.FromObject(this.Subject));
            if (this.Audience != null)
                json.Add(nameof(JwtOpenIDPayload.Audience), JToken.FromObject(this.Audience));
            if (this.UserID != null)
                json.Add(nameof(JwtOpenIDPayload.AAD_ObjectID), JToken.FromObject(this.UserID));
            if (this.UserName != null)
            {
                json.Add(nameof(JwtOpenIDPayload.UniqueName), JToken.FromObject(this.UserName));
                json.Add(nameof(JwtOpenIDPayload.AAD_UserPrincipalName), JToken.FromObject(this.UserName));
            }
            if (this.Name != null)
                json.Add(nameof(JwtOpenIDPayload.AAD_Name), JToken.FromObject(this.Name));
            if (this.Roles != null)
                json.Add(nameof(JwtOpenIDPayload.Roles), JToken.FromObject(this.Roles));
            if (this.Emails != null)
                json.Add(nameof(JwtOpenIDPayload.AAD_Emails), JToken.FromObject(this.Emails));

            if (this.Nonce != null)
                json.Add(nameof(JwtOpenIDPayload.Nonce), JToken.FromObject(this.Nonce));
            if (this.IssuedAtTime != null)
                json.Add(nameof(JwtOpenIDPayload.IssuedAtTime), JToken.FromObject(this.IssuedAtTime));
            if (this.NotBefore != null)
                json.Add(nameof(JwtOpenIDPayload.NotBefore), JToken.FromObject(this.NotBefore));
            if (this.Expiration != null)
                json.Add(nameof(JwtOpenIDPayload.Expiration), JToken.FromObject(this.Expiration));

            if (this.X509Thumbprint != null)
                json.Add(nameof(JwtHeader.X509Thumbprint), JToken.FromObject(this.X509Thumbprint));
            if (this.KeyID != null)
                json.Add(nameof(JwtHeader.KeyID), JToken.FromObject(this.KeyID));

            if (this.State != null)
                json.Add("state", JToken.FromObject(this.State));

            if (this.OtherClaims != null)
            {
                foreach (var claim in this.OtherClaims)
                {
                    if (!json.ContainsKey(claim.Key))
                        json.Add(claim.Key, JToken.FromObject(claim.Value));
                }
            }

            return json;
        }
    }
}