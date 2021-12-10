// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Bindings
{
    internal class Saml2FormBinding : Saml2Binding
    {
        public override BindingType BindingType => BindingType.Form;

        public SignatureAlgorithm? SignatureAlgorithm { get; protected set; }
        public DigestAlgorithm? DigestAlgorithm { get; protected set; }
        public EncryptionAlgorithm? EncryptionAlgorithm { get; protected set; }
        public bool HasSignature { get; protected set; }
        public bool HasEncryption { get; protected set; }

        internal Saml2FormBinding(Saml2Document document, SignatureAlgorithm? signatureAlgorithm, DigestAlgorithm? digestAlgorithm, EncryptionAlgorithm? encryptionAlgorithm)
        {
            this.BindingDirection = document.BindingDirection;
            this.SignatureAlgorithm = signatureAlgorithm;
            this.DigestAlgorithm = digestAlgorithm;
            this.EncryptionAlgorithm = encryptionAlgorithm;

            this.Document = document.GetSaml();

            this.HasSignature = X509XmlSigner.HasSignature(this.Document.DocumentElement);
            if (this.HasSignature)
            {
                this.SignatureAlgorithm = X509XmlSigner.GetSignatureAlgorithm(this.Document.DocumentElement);
                this.DigestAlgorithm = X509XmlSigner.GetDigestAlgorithm(this.Document.DocumentElement);
            }

            this.HasEncryption = X509XmlEncryptor.HasEncryptedDataElements(this.Document.DocumentElement);
            if (this.HasEncryption)
            {
                this.EncryptionAlgorithm = X509XmlEncryptor.GetEncryptionAlgorithm(this.Document.DocumentElement);
            }
        }

        internal Saml2FormBinding(HttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;
            string samlEncoded = this.BindingDirection switch
            {
                BindingDirection.Request => request.Form[Saml2Names.RequestParameterName],
                BindingDirection.Response => request.Form[Saml2Names.ResponseParameterName],
                _ => throw new NotImplementedException(),
            };
            var samlRequestDecoded = DecodeSaml(samlEncoded);
            this.Document = new XmlDocument();
            this.Document.LoadXml(samlRequestDecoded);

            this.HasSignature = X509XmlSigner.HasSignature(this.Document.DocumentElement);
            if (this.HasSignature)
            {
                this.SignatureAlgorithm = X509XmlSigner.GetSignatureAlgorithm(this.Document.DocumentElement);
                this.DigestAlgorithm = X509XmlSigner.GetDigestAlgorithm(this.Document.DocumentElement);
            }

            this.HasEncryption = X509XmlEncryptor.HasEncryptedDataElements(this.Document.DocumentElement);
            if (this.HasEncryption)
            {
                this.EncryptionAlgorithm = X509XmlEncryptor.GetEncryptionAlgorithm(this.Document.DocumentElement);
            }
        }

        public override void Sign(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new InvalidOperationException("Saml2 Missing Cert for Required Signing");

            if (this.HasSignature)
                throw new InvalidOperationException("Saml2 Document is Already Signed");

            if (cert == null)
                return;

            if (this.SignatureAlgorithm == null)
                this.SignatureAlgorithm = Cryptography.SignatureAlgorithm.RsaSha256;
            if (this.DigestAlgorithm == null)
                this.DigestAlgorithm = Cryptography.DigestAlgorithm.Sha256;

            this.Document = X509XmlSigner.SignXmlDoc(this.Document, cert, this.SignatureAlgorithm.Value, this.DigestAlgorithm.Value);
            this.HasSignature = true;
        }

        public override void ValidateSignature(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new InvalidOperationException("Saml2 Missing Cert for Validating Required Signature");

            if (requiredSignature && !this.HasSignature)
                throw new IdentityProviderException("Saml2 Document Missing Required Signature");

            if (cert == null)
                return;

            if (this.HasSignature)
            {
                var validSignature = X509XmlSigner.Validate(this.Document, cert);
                if (!validSignature)
                    throw new IdentityProviderException("Saml2 Document Signature Not Valid");
            }
        }

        public override void Encrypt(X509Certificate2 cert, bool requiredEncryption)
        {
            if (requiredEncryption && cert == null)
                throw new InvalidOperationException("Saml2 Missing Cert for Required Encryption");

            if (this.HasSignature)
                throw new InvalidOperationException("Saml2 Document is already signed");
            if (this.HasEncryption)
                throw new InvalidOperationException("Saml2 Document is already encrypted");

            if (this.EncryptionAlgorithm == null)
                this.EncryptionAlgorithm = Cryptography.EncryptionAlgorithm.Aes128Cbc;

            this.Document = X509XmlEncryptor.EncryptXmlDoc(this.Document, cert, this.EncryptionAlgorithm.Value, Saml2Names.AssertionPrefix, "Assertion", Saml2Names.AssertionPrefix, "EncryptedAssertion");
            this.HasEncryption = true;
        }

        public override void Decrypt(X509Certificate2 cert, bool requiredEncryption)
        {
            if (requiredEncryption && cert == null)
                throw new IdentityProviderException("Saml2 Missing Cert for Decryption");

            if (requiredEncryption && !this.HasEncryption)
                throw new IdentityProviderException("Saml2 Document Missing Required Encryption");

            if (this.HasEncryption)
            {
                this.Document = X509XmlEncryptor.DecryptXmlDoc(this.Document, cert);
                this.HasEncryption = false;
            }
        }

        private static string EncodeSaml(string saml)
        {
            var bytes = Encoding.UTF8.GetBytes(saml);
            return Convert.ToBase64String(bytes);
        }

        private static string DecodeSaml(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private string GetPostContent(string url)
        {
            var inputs = new Dictionary<string, string>();

            var samlEncoded = EncodeSaml(this.Document.InnerXml);

            switch (this.BindingDirection)
            {
                case BindingDirection.Request:
                    inputs.Add(Saml2Names.RequestParameterName, samlEncoded);
                    break;
                case BindingDirection.Response:
                    inputs.Add(Saml2Names.ResponseParameterName, samlEncoded);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return GeneratePost(url, inputs);
        }
        private static string GeneratePost(string url, IEnumerable<KeyValuePair<string, string>> inputs)
        {
            var sb = new StringBuilder();

            sb.Append("<html><head><title>Working...</title></head><body>");
            sb.Append("<form method=\"POST\" name=\"name=\"hiddenform\" action=\"").Append(url).Append("\">");
            if (inputs != null)
            {
                foreach (KeyValuePair<string, string> input in inputs)
                    sb.Append(String.Format("<input type=\"hidden\" name=\"{0}\" value=\"{1}\">", WebUtility.HtmlEncode(input.Key), WebUtility.HtmlEncode(input.Value)));
            }
            sb.Append("<noscript><p>Script is disabled. Click Submit to continue.</p><input type=\"submit\" value=\"Submit\" /></noscript>");
            sb.Append("</form>");
            sb.Append("<script language=\"javascript\">document.forms[0].submit();</script>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        public override string GetContent()
        {
            var inputs = new Dictionary<string, string>();

            var samlEncoded = EncodeSaml(this.Document.InnerXml);

            switch (this.BindingDirection)
            {
                case BindingDirection.Request:
                    inputs.Add(Saml2Names.RequestParameterName, samlEncoded);
                    break;
                case BindingDirection.Response:
                    inputs.Add(Saml2Names.ResponseParameterName, samlEncoded);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var sb = new StringBuilder();

            foreach (var input in inputs)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(WebUtility.HtmlEncode(input.Key)).Append('=').Append(WebUtility.HtmlEncode(input.Value));
            }

            return sb.ToString();
        }

        public override IActionResult GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            var content = GetPostContent(url);
            return new ContentResult()
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = content
            };
        }
    }
}
