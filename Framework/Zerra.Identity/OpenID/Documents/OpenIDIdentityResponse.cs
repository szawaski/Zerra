// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.TokenManagers;
using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDIdentityResponse : OpenIDDocument
    {
        public string ServiceProvider { get; private set; }
        public string UserID { get; private set; }
        public string UserName { get; private set; }
        public string[] Roles { get; private set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OpenIDIdentityResponse(string serviceProvider, string  token)
        {
            var identity = AuthTokenManager.GetIdentity(serviceProvider, token);
            if (identity == null)
                throw new IdentityProviderException("Invalid token");

            this.ServiceProvider = serviceProvider;
            this.UserID = identity.UserID;
            this.UserName = identity.UserName;
            this.Roles = identity.Roles;
        }

        public OpenIDIdentityResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();
            this.UserID = json["userid"]?.ToObject<string>();
            this.UserName = json["username"]?.ToObject<string>();
            this.Roles = json["roles"]?.ToObject<string[]>();
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.UserID != null)
                json.Add("userid", JToken.FromObject(this.UserID));
            if (this.UserName != null)
                json.Add("username", JToken.FromObject(this.UserName));
            if (this.Roles != null)
                json.Add("roles", JToken.FromObject(this.Roles));

            return json;
        }
    }
}

