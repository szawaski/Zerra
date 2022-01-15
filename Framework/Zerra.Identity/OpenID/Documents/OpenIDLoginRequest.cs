// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public class OpenIDLoginRequest : OpenIDDocument
    {
        public string ServiceProvider { get; protected set; }
        public string RedirectUrl { get; protected set; }
        public BindingType BindingType { get; protected set; }
        public OpenIDResponseType? ResponseType { get; protected set; }
        public OpenIDResponseMode? ResponseMode { get; protected set; }
        public string Scope { get; protected set; }
        public string State { get; protected set; }
        public string Nonce { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDLoginRequest(string serviceProvider, string redirectUrl, OpenIDResponseType? responseType, OpenIDResponseMode? responseMode, BindingType bindingType, string scope, string state, string nonce)
        {
            this.ServiceProvider = serviceProvider;
            this.RedirectUrl = redirectUrl;
            this.BindingType = bindingType;
            this.ResponseType = responseType;
            this.ResponseMode = responseMode;
            this.Scope = scope;
            this.State = state;
            this.Nonce = nonce;
        }

        public OpenIDLoginRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.ServiceProvider = json[OpenIDBinding.ClientFormName]?.ToObject<string>();
            this.RedirectUrl = json["redirect_uri"]?.ToObject<string>();

            if (!String.IsNullOrWhiteSpace(json["response_type"]?.ToObject<string>()))
                this.ResponseType = EnumName.Parse<OpenIDResponseType>(json["response_type"]?.ToObject<string>());
            else
                this.ResponseType = OpenIDResponseType.IdToken;

            if (!String.IsNullOrWhiteSpace(json["response_mode"]?.ToObject<string>()))
                this.ResponseMode = EnumName.Parse<OpenIDResponseMode>(json["response_mode"]?.ToObject<string>());
            else
                this.ResponseMode = OpenIDResponseMode.form_post;

            this.Scope = json["scope"]?.ToObject<string>();
            this.State = json["state"]?.ToObject<string>();
            this.Nonce = json["nonce"]?.ToObject<string>();

            switch (this.ResponseMode)
            {
                case OpenIDResponseMode.form_post: this.BindingType = BindingType.Form; break;
                case OpenIDResponseMode.query: this.BindingType = BindingType.Query; break;
                case OpenIDResponseMode.fragment: throw new IdentityProviderException("Response Mode fragment is not supported");
            }
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            switch (this.BindingType)
            {
                case BindingType.Form: this.ResponseMode  = OpenIDResponseMode.form_post; break;
                case BindingType.Query: this.ResponseMode = OpenIDResponseMode.query; break;
                case BindingType.Stream: throw new IdentityProviderException("Binding Type content is not supported");
            }

            if (this.ServiceProvider != null)
                json.Add(OpenIDBinding.ClientFormName, JToken.FromObject(this.ServiceProvider));
            if (this.RedirectUrl != null)
                json.Add("redirect_uri", JToken.FromObject(this.RedirectUrl));
            if (this.ResponseMode != null)
                json.Add("response_mode", JToken.FromObject(this.ResponseMode?.EnumName()));
            if (this.ResponseType != null)
                json.Add("response_type", JToken.FromObject(this.ResponseType?.EnumName()));
            if (this.Scope != null)
                json.Add("scope", JToken.FromObject(this.Scope));
            if (this.State != null)
                json.Add("state", JToken.FromObject(this.State));
            if (this.Nonce != null)
                json.Add("nonce", JToken.FromObject(this.Nonce));

            return json;
        }
    }
}
