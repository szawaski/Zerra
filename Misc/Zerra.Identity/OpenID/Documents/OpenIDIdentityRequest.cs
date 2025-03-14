﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDIdentityRequest : OpenIDDocument
    {
        public string ServiceProvider { get; }
        public string Token { get; }

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

            if (json is null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();
            this.Token = json["token"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider is not null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.Token is not null)
                json.Add("token", JToken.FromObject(this.Token));

            return json;
        }
    }
}

