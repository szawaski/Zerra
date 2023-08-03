// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace Zerra.Identity.OAuth2.Documents
{
    public sealed class OAuth2LogoutRequest : OAuth2Document
    {
        public string ServiceProvider { get; private set; }
        public string RedirectUrlPostLogout { get; private set; }
        public string State { get; private set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OAuth2LogoutRequest(string serviceProvider, string redirectUrl, string state)
        {
            this.ServiceProvider = serviceProvider;
            this.RedirectUrlPostLogout = redirectUrl;
            this.State = state;
        }

        public OAuth2LogoutRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OAuth2Binding.ClientFormName]?.ToObject<string>();

            var redirectUrlPostLogout = json["redirect"]?.ToObject<string>();
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
                json.Add(OAuth2Binding.ClientFormName, JToken.FromObject(this.ServiceProvider));

            if (this.RedirectUrlPostLogout != null)
            {
                var redirectUrlPostLogout = this.RedirectUrlPostLogout + "?state=" + WebUtility.UrlEncode(this.State);
                json.Add("redirect", JToken.FromObject(redirectUrlPostLogout));
            }

            return json;
        }
    }
}
