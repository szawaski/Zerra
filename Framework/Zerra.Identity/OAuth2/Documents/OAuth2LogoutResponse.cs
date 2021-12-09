// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Zerra.Identity.OAuth2.Documents
{
    public class OAuth2LogoutResponse : OAuth2Document
    {
        public string ServiceProvider { get; protected set; }
        public string State { get; protected set; }
        public Dictionary<string, string> OtherClaims { get; set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OAuth2LogoutResponse(string serviceProvider, string state)
        {
            this.ServiceProvider = serviceProvider;
        }

        public OAuth2LogoutResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();
            this.State = json["state"]?.ToObject<string>();

            this.OtherClaims = OAuth2Binding.GetOtherClaims(json);
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.State != null)
                json.Add("state", JToken.FromObject(this.ServiceProvider));

            if (this.OtherClaims != null)
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
