// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDTokenRequest : OpenIDDocument
    {
        public OpenIDGrantType? GrantType { get; }
        public string ClientId { get; }
        public string Code { get; }
        public string Secret { get; }
        public string RedirectUrl { get; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDTokenRequest(string clientId, string code, string secret, OpenIDGrantType? grantType, string redirectUrl)
        {
            this.ClientId = clientId;
            this.Code = code;
            this.Secret = secret;
            this.GrantType = grantType;
            this.RedirectUrl = redirectUrl;
        }

        public OpenIDTokenRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json is null)
                return;

            this.ClientId = json["client_id"]?.ToObject<string>();
            this.Code = json["code"]?.ToObject<string>();
            this.Secret = json["client_secret"]?.ToObject<string>();

            if (!String.IsNullOrWhiteSpace(json["grant_type"]?.ToObject<string>()))
                this.GrantType = EnumName.Parse<OpenIDGrantType>(json["grant_type"]?.ToObject<string>());

            this.RedirectUrl = json["redirect_uri"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.GrantType is not null)
                json.Add("grant_type", JToken.FromObject(this.GrantType?.EnumName()));
            if (this.ClientId is not null)
                json.Add("client_id", JToken.FromObject(this.ClientId));
            if (this.Code is not null)
                json.Add("code", JToken.FromObject(this.Code));
            if (this.Secret is not null)
                json.Add("client_secret", JToken.FromObject(this.Secret));
            if (this.RedirectUrl is not null)
                json.Add("redirect_uri", JToken.FromObject(this.RedirectUrl));

            return json;
        }
    }
}

