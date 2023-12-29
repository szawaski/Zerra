// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDIdentityRequest : OpenIDDocument
    {
        public string ServiceProvider { get; private set; }
        public string Token { get; private set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDIdentityRequest(string serviceProvider, string token)
        {
            this.ServiceProvider = serviceProvider;
            this.Token = token;
        }

        public OpenIDIdentityRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();
            this.Token = json["token"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.Token != null)
                json.Add("token", JToken.FromObject(this.Token));

            return json;
        }
    }
}

