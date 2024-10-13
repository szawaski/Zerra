// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Zerra.Identity.Cryptography
{
    public static class X509XmlSigner
    {
        static X509XmlSigner()
        {
            ConfigSignatureDescriptions.Add();
        }

        public static XmlDocument SignXmlDoc(XmlDocument xmlDoc, X509Certificate2 cert, XmlSignatureAlgorithmType signatureAlgorithm, XmlDigestAlgorithmType digestAlgorithm)
        {
            var signedXml = GenerateSignedXml(xmlDoc, cert, signatureAlgorithm, digestAlgorithm);

            _ = xmlDoc.DocumentElement.AppendChild(signedXml);

            return xmlDoc;
        }

        private static XmlElement GenerateSignedXml(XmlDocument xmlDoc, X509Certificate2 cert, XmlSignatureAlgorithmType signatureAlgorithm, XmlDigestAlgorithmType digestAlgorithm)
        {
            var rsa = cert.GetRSAPrivateKey();
            if (rsa is null)
                throw new IdentityProviderException("X509 must be RSA");

            var signatureAlgorithmUrl = Algorithms.GetSignatureAlgorithmUrl(signatureAlgorithm);
            var digestAlgorithmUrl = Algorithms.GetDigestAlgorithmUrl(digestAlgorithm);

            var signedXml = new PrefixedSignedXml(xmlDoc)
            {
                SigningKey = rsa
            };
            signedXml.SignedInfo.SignatureMethod = signatureAlgorithmUrl;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            //Empty string means entire document, use '#' before name //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.xml.reference.uri?view=netframework-4.7.2
            var referenceUri = String.Empty;
            var id = xmlDoc.DocumentElement.GetAttribute("ID");
            if (!String.IsNullOrWhiteSpace(id))
                referenceUri = "#" + id;

            var reference = new Reference
            {
                Uri = referenceUri,
                DigestMethod = digestAlgorithmUrl
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(reference);

            signedXml.KeyInfo = new KeyInfo();
            signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert));

            signedXml.ComputeSignature("ds");

            var signedXmlDoc = signedXml.GetXml("ds");

            return signedXmlDoc;
        }

        public static bool Validate(XmlDocument xmlDoc, X509Certificate2 cert)
        {
            var signatureElement = GetSignatureElement(xmlDoc.DocumentElement);

            var signedXml = new SignedXml(xmlDoc);
            signedXml.LoadXml(signatureElement);

            var valid = signedXml.CheckSignature(cert.GetRSAPublicKey());
            return valid;
        }

        public static bool HasSignature(XmlElement element)
        {
            return GetSignatureElement(element) is not null;
        }

        private static XmlElement GetSignatureElement(XmlElement element)
        {
            return element.GetSingleElement(null, "Signature", true);
        }

        public static XmlSignatureAlgorithmType GetSignatureAlgorithm(XmlElement element)
        {
            var signatureElement = GetSignatureElement(element);
            var signedInfo = signatureElement.GetSingleElementRequired(null, "SignedInfo", false);
            var signatureMethod = signedInfo.GetSingleElementRequired(null, "SignatureMethod", false);
            var algorithmUrl = signatureMethod.GetAttributeRequired("Algorithm");
            var signatureAlgorithm = Algorithms.GetSignatureAlgorithmFromUrl(algorithmUrl);
            return signatureAlgorithm;
        }

        public static XmlDigestAlgorithmType GetDigestAlgorithm(XmlElement element)
        {
            var signatureElement = GetSignatureElement(element);
            var signedInfo = signatureElement.GetSingleElementRequired(null, "SignedInfo", false);
            var reference = signedInfo.GetSingleElementRequired(null, "Reference", false);
            var digestMethod = reference.GetSingleElementRequired(null, "DigestMethod", false);
            var digestAlgorithmUrl = digestMethod.GetAttributeRequired("Algorithm");
            var digestAlgorithm = Algorithms.GetDigestAlgorithmFromUrl(digestAlgorithmUrl);
            return digestAlgorithm;
        }
    }
}
