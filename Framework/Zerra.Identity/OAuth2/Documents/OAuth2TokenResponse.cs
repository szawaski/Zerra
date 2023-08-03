﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.TokenManagers;
using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OAuth2.Documents
{
    public sealed class OAuth2TokenResponse : OAuth2Document
    {
        public string ServiceProvider { get; private set; }
        public string Token { get; private set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OAuth2TokenResponse(string serviceProvider, string code)
        {
            var token = AuthTokenManager.GetToken(serviceProvider, code);
            if (token == null)
                throw new IdentityProviderException("Invalid access code");

            this.ServiceProvider = serviceProvider;
            this.Token = token;
        }

        public OAuth2TokenResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();
            this.Token = json["token"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.Token != null)
                json.Add("token", JToken.FromObject(this.Token));

            return json;
        }
    }
}

