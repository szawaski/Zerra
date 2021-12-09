// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public class OpenIDTokenRequest : OpenIDDocument
    {
        public string GrantType { get; protected set; }
        public string Code { get; protected set; }
        public string RedirectUrl { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDTokenRequest(string code, string redirectUrl)
        {
            this.GrantType = "authorization_code";
            this.Code = code;
            this.RedirectUrl = redirectUrl;
        }

        public OpenIDTokenRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.GrantType = json["grant_type"]?.ToObject<string>();
            this.Code = json["code"]?.ToObject<string>();
            this.RedirectUrl = json["redirect_uri"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.GrantType != null)
                json.Add("grant_type", JToken.FromObject(this.GrantType));
            if (this.Code != null)
                json.Add("code", JToken.FromObject(this.Code));
            if (this.RedirectUrl != null)
                json.Add("redirect_uri", JToken.FromObject(this.RedirectUrl));

            return json;
        }
    }
}

