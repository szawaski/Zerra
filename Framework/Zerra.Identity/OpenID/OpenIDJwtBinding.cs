// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Zerra.Identity.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zerra.Identity.OpenID.Bindings;
using System.Security.Cryptography;
using System.Net;
using System.Reflection;
using Zerra.Encryption;

namespace Zerra.Identity.OpenID
{
    public abstract class OpenIDJwtBinding : Binding<JObject>
    {
        protected const string IdTokenFormName = "id_token";
        protected const string AccessTokenFormName = "access_token";

        public abstract string AccessToken { get; }

        public static bool IsCodeBinding(IdentityHttpRequest request)
        {
            if (request.HasFormContentType)
                return request.Form.Keys.Contains("code");
            else
                return request.Query.Keys.Contains("code");
        }

        public static OpenIDJwtBinding GetBindingForRequest(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            if (request.HasFormContentType)
                return new OpenIDJwtFormBinding(request, bindingDirection);
            else
                return new OpenIDJwtQueryBinding(request, bindingDirection);
        }

        public static OpenIDJwtBinding GetBindingForResponse(WebResponse request, BindingDirection flowDirection)
        {
            return new OpenIDJwtStreamBinding(request, flowDirection);
        }

        public static OpenIDJwtBinding GetBindingForDocument(OpenIDDocument document, BindingType bindingType, XmlSignatureAlgorithmType? signatureAlgorithm)
        {
            return bindingType switch
            {
                BindingType.Null => throw new IdentityProviderException("Cannot have null binding type"),
                BindingType.Form => new OpenIDJwtFormBinding(document, signatureAlgorithm),
                BindingType.Query => new OpenIDJwtQueryBinding(document, signatureAlgorithm),
                BindingType.Stream => throw new IdentityProviderException("Json Web Tokens do not support content binding"),
                _ => throw new NotImplementedException(),
            };
        }

        public XmlSignatureAlgorithmType? SignatureAlgorithm { get; protected set; }
        public string Signature { get; protected set; }

        protected string singingInput = null;

        protected const string tokenType = "JWT";
        protected const char tokenDelimiter = '.';

        public void Sign(RSA rsa, bool requiredSignature)
        {
            if (requiredSignature && rsa == null)
                throw new InvalidOperationException("OpenID Missing Cert for Required Signing");

            if (this.Signature != null)
                throw new InvalidOperationException("OpenID Document is Already Signed");

            if (rsa == null)
                return;

            this.singingInput = BuildSigningInput();

            this.Signature = TextSigner.GenerateSignatureString(this.singingInput, rsa, this.SignatureAlgorithm.Value, true);
        }

        public void ValidateSignature(RSA rsa, bool requiredSignature)
        {
            if (requiredSignature && rsa == null)
                throw new InvalidOperationException("OpenID Missing Cert for Validating Required Signature");

            if (requiredSignature && this.Signature == null)
                throw new IdentityProviderException("OpenID Document Missing Required Signature");

            if (rsa == null)
                return;

            if (this.Signature != null)
            {
                var valid = TextSigner.Validate(this.singingInput, this.Signature, rsa, this.SignatureAlgorithm.Value, true);
                if (!valid)
                    throw new IdentityProviderException("OpenID Document Signature Not Valid");
            }
        }

        public void ValidateFields()
        {
            var valid = true;

            var notBefore = NotBefore(this.Document);
            valid &= !notBefore.HasValue || (notBefore.Value.AddSeconds(-5) <= DateTimeOffset.UtcNow);

            var notOnOrAfter = NotOnOrAfter(this.Document);
            valid &= !notOnOrAfter.HasValue || (notOnOrAfter > DateTimeOffset.UtcNow);

            if (!valid)
                throw new IdentityProviderException("OpenID Document Is Invalid Or Expired");

            var error = Error(this.Document);
            if (error != null)
            {
                var errorDescription = ErrorDescription(this.Document);
                throw new IdentityProviderException(errorDescription);
            }
        }
        private static DateTimeOffset? NotBefore(JObject json)
        {
            var value = json[nameof(JwtOpenIDPayload.NotBefore)]?.ToObject<string>();
            if (value == null)
                return null;
            var date = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));
            return date;
        }
        private static DateTimeOffset? NotOnOrAfter(JObject json)
        {
            var value = json[nameof(JwtOpenIDPayload.Expiration)]?.ToObject<string>();
            if (value == null)
                return null;
            var date = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));
            return date;
        }
        private static string Error(JObject json)
        {
            var value = json["error"]?.ToObject<string>();
            return value;
        }
        private static string ErrorDescription(JObject json)
        {
            var value = json["error_description"]?.ToObject<string>();
            return value;
        }

        protected JwtHeader BuildJwtHeader()
        {
            var jwtHeader = new JwtHeader()
            {
                Type = OpenIDJwtBinding.tokenType,
                Algorithm = this.SignatureAlgorithm.HasValue ? Algorithms.GetSignatureAlgorithmJwt(this.SignatureAlgorithm.Value) : null,
                KeyID = this.Document[nameof(JwtHeader.KeyID)]?.ToObject<string>(),
                X509Thumbprint = this.Document[nameof(JwtHeader.X509Thumbprint)]?.ToObject<string>()
            };
            return jwtHeader;
        }

        protected JwtOpenIDPayload BuildJwtPayload()
        {
            var jwtPayload = new JwtOpenIDPayload();
            foreach (var property in typeof(JwtOpenIDPayload).GetProperties())
            {
                var jToken = this.Document[property.Name];
                if (jToken != null)
                {
                    var value = jToken.ToObject(property.PropertyType);
                    property.SetValue(jwtPayload, value);
                }
            }
            return jwtPayload;
        }

        protected string BuildSigningInput()
        {
            var jwtHeader = BuildJwtHeader();
            var jwtPayload = BuildJwtPayload();

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var jwtHeaderString = JsonConvert.SerializeObject(jwtHeader, jsonSettings);
            var jwtPayloadString = JsonConvert.SerializeObject(jwtPayload, jsonSettings);

            var jwtHeaderEncoded = EncodeJwt(jwtHeaderString);
            var jwtPayloadEncoded = EncodeJwt(jwtPayloadString);

            StringBuilder sb = new StringBuilder();
            sb.Append(jwtHeaderEncoded);
            sb.Append(OpenIDJwtFormBinding.tokenDelimiter);
            sb.Append(jwtPayloadEncoded);
            return sb.ToString();
        }

        protected string BuildToken()
        {
            var jwtHeader = BuildJwtHeader();
            var jwtPayload = BuildJwtPayload();

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var jwtHeaderString = JsonConvert.SerializeObject(jwtHeader, jsonSettings);
            var jwtPayloadString = JsonConvert.SerializeObject(jwtPayload, jsonSettings);

            var jwtHeaderEncoded = EncodeJwt(jwtHeaderString);
            var jwtPayloadEncoded = EncodeJwt(jwtPayloadString);

            StringBuilder sb = new StringBuilder();
            sb.Append(jwtHeaderEncoded);
            sb.Append(OpenIDJwtFormBinding.tokenDelimiter);
            sb.Append(jwtPayloadEncoded);
            if (this.Signature != null)
            {
                sb.Append(OpenIDJwtFormBinding.tokenDelimiter);
                sb.Append(this.Signature);
            }
            return sb.ToString();
        }

        private static readonly object jwtPayloadPropertiesLock = new object();
        private static Dictionary<string, string> jwtPayloadPropertiesCache = null;
        private static Dictionary<string, string> GetJwtPayloadProperties()
        {
            if (jwtPayloadPropertiesCache == null)
            {
                lock (jwtPayloadPropertiesLock)
                {
                    if (jwtPayloadPropertiesCache == null)
                    {
                        var cache = new Dictionary<string, string>();
                        var properties = typeof(JwtOpenIDPayload).GetProperties();
                        foreach (var property in properties)
                        {
                            var attribute = (JsonPropertyAttribute)property.GetCustomAttribute(typeof(JsonPropertyAttribute));
                            cache.Add(attribute?.PropertyName ?? property.Name, property.Name);
                        }
                        jwtPayloadPropertiesCache = cache;
                    }
                }
            }
            return jwtPayloadPropertiesCache;
        }

        internal void DeserializeJwtPayload(string jwtPayloadString)
        {
            var jwtPayload = JObject.Parse(jwtPayloadString);

            this.Document = new JObject();
            var jwtProperties = GetJwtPayloadProperties();

            foreach (var jwtProperty in jwtPayload.Properties())
            {
                if (jwtProperties.TryGetValue(jwtProperty.Name, out string propertyName))
                    this.Document.Add(propertyName, jwtProperty.Value);
                else
                    this.Document.Add(jwtProperty);
            }
        }

        internal static Dictionary<string, string> GetOtherClaims(JObject document)
        {
            var otherFields = new Dictionary<string, string>();
            var usedKeys = typeof(JwtHeader).GetProperties().Select(x => x.Name).Concat(
                typeof(JwtOpenIDPayload).GetProperties().Select(x => x.Name)).ToArray();

            foreach (var item in document)
            {
                if (!usedKeys.Contains(item.Key))
                {
                    if (item.Value.Type == JTokenType.Array)
                    {
                        otherFields.Add(item.Key, String.Join(", ", item.Value.ToObject<string[]>()));
                    }
                    else
                    {
                        otherFields.Add(item.Key, item.Value.ToObject<string>());
                    }
                }
            }

            return otherFields;
        }

        protected static string EncodeJwt(string jwt)
        {
            var bytes = Encoding.UTF8.GetBytes(jwt);
            return Base64UrlEncoder.ToBase64String(bytes);
        }

        protected static string DecodeJwt(string base64)
        {
            var bytes = Base64UrlEncoder.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
