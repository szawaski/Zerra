// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDLogoutResponse : OpenIDDocument
    {
        public string ServiceProvider { get; }
        public string State { get; }
        public Dictionary<string, string> OtherClaims { get; set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OpenIDLogoutResponse(string serviceProvider, string state)
        {
            this.ServiceProvider = serviceProvider;
            this.State = state;
        }

        public OpenIDLogoutResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json is null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();
            this.State = json["state"]?.ToObject<string>();

            this.OtherClaims = OpenIDJwtBinding.GetOtherClaims(json);
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider is not null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.State is not null)
                json.Add("state", JToken.FromObject(this.ServiceProvider));

            if (this.OtherClaims is not null)
            {
                foreach (var claim in this.OtherClaims)
                {
                    if (!json.ContainsKey(claim.Key))
                        json.Add(claim.Key, JToken.FromObject(claim.Value));
                }
            }

            return json;
        }
    }
}
