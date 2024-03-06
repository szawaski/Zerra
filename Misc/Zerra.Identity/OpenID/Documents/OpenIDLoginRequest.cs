// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;

namespace Zerra.Identity.OpenID.Documents
{
    public sealed class OpenIDLoginRequest : OpenIDDocument
    {
        public string ServiceProvider { get; }
        public string RedirectUrl { get; }
        public BindingType BindingType { get; }
        public OpenIDResponseType? ResponseType { get; }
        public OpenIDResponseMode? ResponseMode { get; private set; }
        public string Scope { get; }
        public string State { get; }
        public string Nonce { get; }
        public string AcrValues { get; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public OpenIDLoginRequest(string serviceProvider, string redirectUrl, OpenIDResponseType? responseType, OpenIDResponseMode? responseMode, BindingType bindingType, string scope, string state, string nonce, string acrValues)
        {
            this.ServiceProvider = serviceProvider;
            this.RedirectUrl = redirectUrl;
            this.BindingType = bindingType;
            this.ResponseType = responseType;
            this.ResponseMode = responseMode;
            this.Scope = scope;
            this.State = state;
            this.Nonce = nonce;
            this.AcrValues = acrValues;
        }

        public OpenIDLoginRequest(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            foreach (var jsonObject in json)
            {
                switch (jsonObject.Key)
                {
                    case OpenIDBinding.ClientFormName:
                        this.ServiceProvider = jsonObject.Value.ToObject<string>();
                        break;
                    case "redirect_uri":
                        this.RedirectUrl = jsonObject.Value.ToObject<string>();
                        break;
                    case "response_type":
                        this.ResponseType = EnumName.Parse<OpenIDResponseType>(jsonObject.Value.ToObject<string>());
                        break;
                    case "response_mode":
                        this.ResponseMode = EnumName.Parse<OpenIDResponseMode>(jsonObject.Value.ToObject<string>());
                        break;
                    case "scope":
                        this.Scope = jsonObject.Value.ToObject<string>();
                        break;
                    case "state":
                        this.State = jsonObject.Value.ToObject<string>();
                        break;
                    case "nonce":
                        this.Nonce = jsonObject.Value.ToObject<string>();
                        break;
                    case "acr_values":
                        this.AcrValues = jsonObject.Value.ToObject<string>();
                        break;
                }
            }
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
                case BindingType.Form: this.ResponseMode = OpenIDResponseMode.form_post; break;
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
            if (this.AcrValues != null)
                json.Add("acr_values", JToken.FromObject(this.AcrValues));

            return json;
        }
    }
}
