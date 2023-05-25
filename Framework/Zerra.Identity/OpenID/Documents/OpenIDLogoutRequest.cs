// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDLogoutRequest : OpenIDDocument
    {
        public string ServiceProvider { get; protected set; }
        public string RedirectUrlPostLogout { get; protected set; }
        public string State { get; protected set; }

        public Dictionary<string, string> OtherClaims { get; set; }

        public string Error { get; protected set; }
        public string ErrorDescription { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDLogoutRequest(string serviceProvider, string redirectUrl, string state)
        {
            this.ServiceProvider = serviceProvider;
            this.RedirectUrlPostLogout = redirectUrl;
            this.State = state;
        }

        public OpenIDLogoutRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();

            var redirectUrlPostLogout = json["post_logout_redirect_uri"]?.ToObject<string>();
            if (redirectUrlPostLogout != null)
            {
                var redirectUrlPostLogoutSplit = redirectUrlPostLogout.Split(new string[] { "?state=" }, StringSplitOptions.None);
                this.RedirectUrlPostLogout = redirectUrlPostLogoutSplit[0];
                if (redirectUrlPostLogoutSplit.Length > 1)
                    this.State = WebUtility.UrlDecode(redirectUrlPostLogoutSplit[1]);
            }
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.ServiceProvider != null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.RedirectUrlPostLogout != null)
            {
                var redirectUrlPostLogout = this.RedirectUrlPostLogout + "?state=" + WebUtility.UrlEncode(this.State);
                json.Add("post_logout_redirect_uri", JToken.FromObject(redirectUrlPostLogout));
            }

            return json;
        }
    }
}
