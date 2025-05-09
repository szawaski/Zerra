﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OAuth2.Documents
{
    public sealed class OAuth2TokenRequest : OAuth2Document
    { 
        public string ServiceProvider { get; }
        public string Code { get; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OAuth2TokenRequest(string serviceProvider, string code)
        {
            this.ServiceProvider = serviceProvider;
            this.Code = code;
        }

        public OAuth2TokenRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json is null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();
            this.Code = json["code"]?.ToObject<string>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider is not null)
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.Code is not null)
                json.Add("code", JToken.FromObject(this.Code));

            return json;
        }
    }
}

