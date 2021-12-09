// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.TokenManagers;
using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public class OpenIDTokenResponse : OpenIDDocument
    {
        public string Code { get; private set; }
        public string Token { get; private set; }
        public string TokenType { get; private set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OpenIDTokenResponse(string code)
        {
            var token = AuthTokenManager.GetToken(nameof(OpenIDTokenResponse), code);
            if (token == null)
                throw new IdentityProviderException("Invalid access code");

            this.Token = token;
            this.TokenType = "Bearer";
        }

        public OpenIDTokenResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.Token = json["access_token"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.Token != null)
                json.Add("access_token", JToken.FromObject(this.Token));

            return json;
        }
    }
}

