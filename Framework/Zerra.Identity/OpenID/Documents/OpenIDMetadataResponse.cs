// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Identity.Cryptography;
using Newtonsoft.Json.Linq;

namespace Zerra.Identity.OpenID.Documents
{
    public class OpenIDMetadataResponse : OpenIDDocument
    {
        public string Issuer { get; protected set; }
        public string LoginUrl { get; protected set; }
        public string TokenUrl { get; protected set; }
        public string UserInfoUrl { get; protected set; }
        public string KeysUrl { get; protected set; }
        public string LogoutUrl { get; protected set; }
        public XmlSignatureAlgorithmType[] SignatureAlgorithms { get; protected set; }

        public string[] ScopesSupported { get; protected set; }
        public OpenIDResponseType[] ResponseTypesSupported { get; protected set; }
        public OpenIDSubjectIdentifier[] SubjectTypeSupported { get; protected set; }
        public OpenIDResponseMode[] ResponseModesSupported { get; protected set; }
        public string[] ClaimsSupported { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public OpenIDMetadataResponse(Binding<JObject> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var json = binding.GetDocument();

            if (json == null)
                return;

            this.Issuer = json["issuer"]?.ToObject<string>();
            this.LoginUrl = json["authorization_endpoint"]?.ToObject<string>();
            this.TokenUrl = json["token_endpoint"]?.ToObject<string>();
            this.UserInfoUrl = json["userinfo_endpoint"]?.ToObject<string>();
            this.KeysUrl = json["jwks_uri"]?.ToObject<string>();
            this.LogoutUrl = json["end_session_endpoint"]?.ToObject<string>();
            this.ScopesSupported = json["scopes_supported"]?.ToObject<string[]>();

            var responseTypesSupported = json["response_types_supported"]?.ToObject<string[]>();
            if (responseTypesSupported != null)
            {
                var items = new List<OpenIDResponseType>();
                foreach (var x in responseTypesSupported)
                {
                    if (EnumName.TryParse(x, out OpenIDResponseType item))
                        items.Add(item);
                }
                this.ResponseTypesSupported = items.ToArray();
            }

            var subjectTypeSupported = json["subject_types_supported"]?.ToObject<string[]>();
            if (subjectTypeSupported != null)
            {
                var items = new List<OpenIDSubjectIdentifier>();
                foreach (var x in subjectTypeSupported)
                {
                    if (EnumName.TryParse(x, out OpenIDSubjectIdentifier item))
                        items.Add(item);
                }
                this.SubjectTypeSupported = items.ToArray();
            }

            var signatureAlgorithms = json["id_token_signing_alg_values_supported"]?.ToObject<string[]>();
            if (signatureAlgorithms != null)
            {
                var items = new List<XmlSignatureAlgorithmType>();
                foreach (var x in signatureAlgorithms)
                {
                    var item = Algorithms.GetSignatureAlgorithmFromJwt(x);
                    items.Add(item);
                }
                this.SignatureAlgorithms = items.ToArray();
            }

            this.ClaimsSupported = json["claims_supported"]?.ToObject<string[]>();

            var responseModesSupported = json["response_modes_supported"]?.ToObject<string[]>();
            if (responseModesSupported != null)
            {
                var items = new List<OpenIDResponseMode>();
                foreach (var x in responseModesSupported)
                {
                    if (EnumName.TryParse(x, out OpenIDResponseMode item))
                        items.Add(item);
                }
                this.ResponseModesSupported = items.ToArray();
            }
        }

        public override JObject GetJson()
        {
            var json = new JObject();

            if (this.Issuer != null)
                json.Add("issuer", JToken.FromObject(this.Issuer));
            if (this.LoginUrl != null)
                json.Add("authorization_endpoint", JToken.FromObject(this.LoginUrl));
            if (this.TokenUrl != null)
                json.Add("token_endpoint", JToken.FromObject(this.TokenUrl));
            if (this.UserInfoUrl != null)
                json.Add("userinfo_endpoint", JToken.FromObject(this.UserInfoUrl));
            if (this.KeysUrl != null)
                json.Add("jwks_uri", JToken.FromObject(this.KeysUrl));
            if (this.LogoutUrl != null)
                json.Add("end_session_endpoint", JToken.FromObject(this.LogoutUrl));

            if (this.ScopesSupported != null)
                json.Add("scopes_supported", JToken.FromObject(this.ScopesSupported));
            if (this.ResponseTypesSupported != null)
                json.Add("response_types_supported", JToken.FromObject(this.ResponseTypesSupported.Select(x => x.EnumName()).ToArray()));
            if (this.SubjectTypeSupported != null)
                json.Add("subject_types_supported", JToken.FromObject(this.SubjectTypeSupported.Select(x => x.EnumName()).ToArray()));
            if (this.SignatureAlgorithms != null)
                json.Add("id_token_signing_alg_values_supported", JToken.FromObject(this.SignatureAlgorithms.Select(x => Algorithms.GetSignatureAlgorithmJwt(x)).ToArray()));
            if (this.ClaimsSupported != null)
                json.Add("claims_supported", JToken.FromObject(this.ClaimsSupported));
            if (this.ResponseModesSupported != null)
                json.Add("response_modes_supported", JToken.FromObject(this.ResponseModesSupported.Select(x => x.EnumName()).ToArray()));

            return json;
        }
    }
}
