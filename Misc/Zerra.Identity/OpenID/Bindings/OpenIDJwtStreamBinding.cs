// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Zerra.Identity.Cryptography;
using Zerra.Identity.Jwt;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDJwtStreamBinding : OpenIDJwtBinding
    {
        public override BindingType BindingType => BindingType.Stream;

        private readonly string accessToken;
        public override string AccessToken { get { return accessToken; } }

        internal OpenIDJwtStreamBinding(OpenIDDocument document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OpenIDJwtStreamBinding(Stream stream, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            using (var sr = new System.IO.StreamReader(stream))
            {
                var body = sr.ReadToEnd();

                this.Document = JObject.Parse(body);

                string token;
                if (this.Document.ContainsKey(OpenIDJwtBinding.IdTokenFormName))
                {
                    token = this.Document[OpenIDJwtBinding.IdTokenFormName]?.ToObject<string>();
                    accessToken = token;
                }
                else if (this.Document.ContainsKey(OpenIDJwtBinding.AccessTokenFormName))
                {
                    token = this.Document[OpenIDJwtBinding.AccessTokenFormName]?.ToObject<string>();
                    accessToken = token;
                }
                else
                {
                    throw new IdentityProviderException("Missing JWT Token");
                }

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

                if (!this.Document.ContainsKey(nameof(JwtHeader.X509Thumbprint)) && jwtHeader.X509Thumbprint is not null)
                    this.Document.Add(nameof(JwtHeader.X509Thumbprint), JToken.FromObject(jwtHeader.X509Thumbprint));
                if (!this.Document.ContainsKey(nameof(JwtHeader.KeyID)) && jwtHeader.KeyID is not null)
                    this.Document.Add(nameof(JwtHeader.KeyID), JToken.FromObject(jwtHeader.KeyID));

                this.SignatureAlgorithm = Algorithms.GetSignatureAlgorithmFromJwt(jwtHeader.Algorithm);
            }
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
