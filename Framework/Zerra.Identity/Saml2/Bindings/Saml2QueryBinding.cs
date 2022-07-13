// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Bindings
{
    internal class Saml2QueryBinding : Saml2Binding
    {
        public override BindingType BindingType => BindingType.Query;

        public XmlSignatureAlgorithmType? SignatureAlgorithm { get; protected set; }
        public string RelayState { get; protected set; }
        public string Signature { get; protected set; }

        protected string singingInput = null;

        internal Saml2QueryBinding(Saml2Document document, XmlSignatureAlgorithmType? signatureAlgorithm)
        {
            this.BindingDirection = document.BindingDirection;
            this.SignatureAlgorithm = signatureAlgorithm;

            this.Document = document.GetSaml();
        }

        internal Saml2QueryBinding(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;
            var samlEncoded = this.BindingDirection switch
            {
                BindingDirection.Request => request.Query[Saml2Names.RequestParameterName],
                BindingDirection.Response => request.Query[Saml2Names.ResponseParameterName],
                _ => throw new NotImplementedException(),
            };

            //var relayState = (string)request.Query[Saml2Names.RelayStateParameterName];
            var sigAlg = (string)request.Query[Saml2Names.SignatureAlgorithmParameterName];
            this.Signature = request.Query[Saml2Names.SignatureParameterName];

            this.singingInput = request.QueryString.Substring(1, request.QueryString.IndexOf("&" + Saml2Names.SignatureParameterName + "=") - 1);

            if (samlEncoded == null)
                return;

            var samlRequestDecoded = DecodeSaml(samlEncoded);
            this.Document = new XmlDocument();
            this.Document.LoadXml(samlRequestDecoded);

            this.SignatureAlgorithm = Algorithms.GetSignatureAlgorithmFromUrl(sigAlg);
        }

        public override void Sign(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new InvalidOperationException("Saml2 Missing Cert for Required Signing");

            if (this.Signature != null)
                throw new InvalidOperationException("Saml2 Document is Already Signed");

            if (this.SignatureAlgorithm == null)
                this.SignatureAlgorithm = Cryptography.XmlSignatureAlgorithmType.RsaSha256;

            var samlEncoded = EncodeSaml(this.Document.InnerXml);
            this.singingInput = BuildSignatureQueryString(samlEncoded);
            this.Signature = TextSigner.GenerateSignatureString(this.singingInput, cert.GetRSAPrivateKey(), this.SignatureAlgorithm.Value, false);
        }

        public override void ValidateSignature(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new InvalidOperationException("Saml2 Missing Cert for Validating Required Signature");

            if (requiredSignature && this.Signature == null)
                throw new IdentityProviderException("Saml2 Document Missing Required Signature");

            if (this.Signature != null)
            {
                var valid = TextSigner.Validate(this.singingInput, this.Signature, cert.GetRSAPublicKey(), this.SignatureAlgorithm.Value, false);
                if (!valid)
                    throw new IdentityProviderException(String.Format("Saml2 Document Signature Not Valid Query:{0} Signature:{1}", this.singingInput, this.Signature));
            }
        }

        public override void Encrypt(X509Certificate2 cert, bool requiredEncryption)
        {
            throw new NotImplementedException("Encryption for this binding not implemented");
        }

        public override void Decrypt(X509Certificate2 cert, bool requiredEncryption)
        {

        }

        private string BuildSignatureQueryString(string samlEncoded)
        {
            var sb = new StringBuilder();

            switch (this.BindingDirection)
            {
                case BindingDirection.Request:
                    sb.Append(Saml2Names.RequestParameterName);
                    break;
                case BindingDirection.Response:
                    sb.Append(Saml2Names.ResponseParameterName);
                    break;
                default:
                    throw new NotImplementedException();
            }

            sb.Append('=').Append(WebUtility.UrlEncode(samlEncoded));
            if (!String.IsNullOrWhiteSpace(this.RelayState))
            {
                sb.Append('&').Append(Saml2Names.RelayStateParameterName).Append('=').Append(this.RelayState);
            }
            if (this.SignatureAlgorithm.HasValue)
            {
                var signatureAlgorithmUrl = Algorithms.GetSignatureAlgorithmUrl(this.SignatureAlgorithm.Value);
                sb.Append('&').Append(Saml2Names.SignatureAlgorithmParameterName).Append('=').Append(WebUtility.UrlEncode(signatureAlgorithmUrl));
            }
            return sb.ToString();
        }

        private static string EncodeSaml(string saml)
        {
            var bytes = Encoding.UTF8.GetBytes(saml);
            using (var msOut = new MemoryStream())
            {
                using (var zip = new DeflateStream(msOut, CompressionMode.Compress))
                {
                    zip.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(msOut.ToArray());
            }
        }

        private static string DecodeSaml(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            using (var msOut = new MemoryStream())
            {
                using (var msIn = new MemoryStream(bytes))
                {
                    using (var zip = new DeflateStream(msIn, CompressionMode.Decompress))
                    {
                        zip.CopyTo(msOut, bytes.Length);
                    }
                    return Encoding.UTF8.GetString(msOut.ToArray());
                }
            }
        }

        private string GetRedirectUrl(string baseUrl)
        {
            var sb = new StringBuilder();
            sb.Append(baseUrl);
            GenerateQueryString(sb);
            return sb.ToString();
        }

        public override string GetContent()
        {
            var sb = new StringBuilder();
            GenerateQueryString(sb);
            return sb.ToString();
        }
        private void GenerateQueryString(StringBuilder sb)
        {
            sb.Append('?');

            var samlEncoded = EncodeSaml(this.Document.InnerXml);
            var queryString = BuildSignatureQueryString(samlEncoded);
            sb.Append(queryString);

            if (this.Signature != null)
            {
                sb.Append('&').Append(Saml2Names.SignatureParameterName).Append('=').Append(WebUtility.UrlEncode(this.Signature));
            }
        }


        public override IdentityHttpResponse GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            var redirectUrl = GetRedirectUrl(url);
            return new IdentityHttpResponse(redirectUrl);
        }
    }
}
