// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using Zerra.Identity.Cryptography;
using Zerra.Identity.Jwt;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDJwtStreamBinding : OpenIDJwtBinding
    {
        public override BindingType BindingType => BindingType.Stream;

        internal OpenIDJwtStreamBinding(OpenIDDocument document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OpenIDJwtStreamBinding(WebResponse response, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            var stream = response.GetResponseStream();
            var sr = new System.IO.StreamReader(stream);
            var body = sr.ReadToEnd();
            response.Close();

            this.Document = JObject.Parse(body);

            var token = this.Document[OpenIDJwtBinding.TokenFormName]?.ToObject<string>();
            if (String.IsNullOrEmpty(token))
                throw new IdentityProviderException("Missing JWT Token");

            var parts = token.Split(OpenIDJwtFormBinding.tokenDelimiter);
            var jwtHeaderString = DecodeJwt(parts[0]);
            var jwtPayloadString = DecodeJwt(parts[1]);
            if (parts.Length > 2 && !String.IsNullOrWhiteSpace(parts[2]))
            {
                this.Signature = parts[2];
            }

            this.singingInput = parts[0] + OpenIDJwtFormBinding.tokenDelimiter + parts[1];

            var jwtHeader = JsonConvert.DeserializeObject<JwtHeader>(jwtHeaderString);
            DeserializeJwtPayload(jwtPayloadString);

            foreach (var child in this.Document.Properties())
            {
                if (child.Name != OpenIDJwtBinding.TokenFormName && !this.Document.ContainsKey(OpenIDJwtBinding.TokenFormName))
                {
                    this.Document.Add(child.Name, child.Value.ToObject<String>());
                }
            }

            if (!this.Document.ContainsKey(nameof(JwtHeader.X509Thumbprint)) && jwtHeader.X509Thumbprint != null)
                this.Document.Add(nameof(JwtHeader.X509Thumbprint), JToken.FromObject(jwtHeader.X509Thumbprint));
            if (!this.Document.ContainsKey(nameof(JwtHeader.KeyID)) && jwtHeader.KeyID != null)
                this.Document.Add(nameof(JwtHeader.KeyID), JToken.FromObject(jwtHeader.KeyID));

            this.SignatureAlgorithm = Algorithms.GetSignatureAlgorithmFromJwt(jwtHeader.Algorithm);
        }

        public override string GetContent()
        {
            var json = this.Document.ToString();
            return json;
        }

        public override IdentityHttpResponse GetResponse(string url = null)
        {
            var content = GetContent();
            return new IdentityHttpResponse("text/html", content);
        }
    }
}
