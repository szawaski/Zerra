// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;

namespace Zerra.Identity.Saml2.Bindings
{
    internal class Saml2StreamBinding : Saml2Binding
    {
        public override BindingType BindingType => BindingType.Stream;

        public XmlSignatureAlgorithmType? SignatureAlgorithm { get; protected set; }
        public XmlDigestAlgorithmType? DigestAlgorithm { get; protected set; }
        public bool HasSignature { get; protected set; }

        internal Saml2StreamBinding(Saml2Document document, XmlSignatureAlgorithmType? signatureAlgorithm = null, XmlDigestAlgorithmType? digestAlgorithm = null)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetSaml();

            this.HasSignature = X509XmlSigner.HasSignature(this.Document.DocumentElement);
        }

        internal Saml2StreamBinding(Stream stream, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            using (var sr = new StreamReader(stream))
            {
                var body = sr.ReadToEnd();

                this.Document = new XmlDocument();
                this.Document.LoadXml(body);

                this.HasSignature = X509XmlSigner.HasSignature(this.Document.DocumentElement);
            }
        }

        public override string GetContent()
        {
            var xml = this.Document.OuterXml;
            return xml;
        }

        public override IdentityHttpResponse GetResponse(string url = null)
        {
            var content = GetContent();
            return new IdentityHttpResponse("text/xml", content);
        }

        public override void Sign(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new IdentityProviderException("Saml2 Missing Cert for Validating Required Signature");

            if (this.HasSignature)
                throw new IdentityProviderException("Saml2 Document is Already Signed");

            if (cert == null)
                return;

            this.SignatureAlgorithm ??= Cryptography.XmlSignatureAlgorithmType.RsaSha256;
            this.DigestAlgorithm ??= Cryptography.XmlDigestAlgorithmType.Sha256;

            this.Document = X509XmlSigner.SignXmlDoc(this.Document, cert, this.SignatureAlgorithm.Value, this.DigestAlgorithm.Value);
            this.HasSignature = true;
        }

        public override void ValidateSignature(X509Certificate2 cert, bool requiredSignature)
        {
            if (requiredSignature && cert == null)
                throw new IdentityProviderException("Saml2 Missing Cert for Validating Required Signature");

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
            throw new NotSupportedException();
        }

        public override void Decrypt(X509Certificate2 cert, bool requiredEncryption)
        {
            throw new NotSupportedException();
        }
    }
}
