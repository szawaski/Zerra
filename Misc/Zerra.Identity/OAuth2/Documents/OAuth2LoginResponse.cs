// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.TokenManagers;
using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OAuth2.Documents
{
    public sealed class OAuth2LoginResponse : OAuth2Document
    {
        public string ServiceProvider { get; }
        public string AccessCode { get; }
        public string State { get; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OAuth2LoginResponse(string serviceProvider, IdentityModel identity, string state)
        {
            this.ServiceProvider = serviceProvider;
            this.AccessCode = AuthTokenManager.GenerateAccessCode(serviceProvider, identity);
            this.State = state;
        }

        public OAuth2LoginResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json is null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();
            this.AccessCode = json["code"]?.ToObject<string>();
            this.State = json["state"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider is not null)
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.AccessCode is not null)
                json.Add("code", JToken.FromObject(this.AccessCode));

            if (this.State is not null)
                json.Add("state", JToken.FromObject(this.State));

            return json;
        }
    }
}
