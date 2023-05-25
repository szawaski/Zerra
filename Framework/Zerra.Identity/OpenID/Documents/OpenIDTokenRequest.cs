// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDTokenRequest : OpenIDDocument
    {
        public OpenIDGrantType? GrantType { get; protected set; }
        public string Code { get; protected set; }
        public string Secret { get; private set; }
        public string RedirectUrl { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDTokenRequest(string code, string secret, OpenIDGrantType? grantType, string redirectUrl)
        {
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

            if (json == null)
                return;

            this.Code = json["code"]?.ToObject<string>();
            this.Secret = json["client_secret"]?.ToObject<string>();

            if (!String.IsNullOrWhiteSpace(json["grant_type"]?.ToObject<string>()))
                this.GrantType = EnumName.Parse<OpenIDGrantType>(json["grant_type"]?.ToObject<string>());

            this.RedirectUrl = json["redirect_uri"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.GrantType != null)
                json.Add("grant_type", JToken.FromObject(this.GrantType?.EnumName()));
            if (this.Code != null)
                json.Add("code", JToken.FromObject(this.Code));
            if (this.Secret != null)
                json.Add("client_secret", JToken.FromObject(this.Secret));
            if (this.RedirectUrl != null)
                json.Add("redirect_uri", JToken.FromObject(this.RedirectUrl));

            return json;
        }
    }
}

