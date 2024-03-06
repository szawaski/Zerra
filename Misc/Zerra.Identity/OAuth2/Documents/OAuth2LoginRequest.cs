// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OAuth2.Documents
{
    public sealed class OAuth2LoginRequest : OAuth2Document
    { 
        public string ServiceProvider { get; }
        public string RedirectUrl { get; }
        public string State { get; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OAuth2LoginRequest(string serviceProvider, string redirectUrl, string state)
        {
            this.ServiceProvider = serviceProvider;
            this.RedirectUrl = redirectUrl;
            this.State = state;
        }

        public OAuth2LoginRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();
            this.RedirectUrl = json["redirect"]?.ToObject<string>();
            this.State = json["state"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.RedirectUrl != null)
                json.Add("redirect", JToken.FromObject(this.RedirectUrl));

            if (this.State != null)
                json.Add("state", JToken.FromObject(this.State));

            return json;
        }
    }
}
