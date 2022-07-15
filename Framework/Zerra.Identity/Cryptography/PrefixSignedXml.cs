// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Zerra.Identity.Cryptography
{
    public class PrefixedSignedXml : SignedXml
    {
        public PrefixedSignedXml(XmlDocument document) : base(document)
        {
        }

        public PrefixedSignedXml(XmlElement element) : base(element)
        {
        }

        public PrefixedSignedXml() : base()
        {
        }

        private static readonly MethodInfo buildDigestedReferencesMethod = typeof(SignedXml).GetMethod("BuildDigestedReferences", BindingFlags.NonPublic | BindingFlags.Instance);
        public void ComputeSignature(string prefix)
        {
            _ = buildDigestedReferencesMethod.Invoke(this, Array.Empty<object>());

            var signingKey = base.SigningKey;
            if (signingKey == null)
                throw new CryptographicException("Cryptography_Xml_LoadKeyFailed");

            if (this.SignedInfo.SignatureMethod == null)
            {
                if (signingKey is DSA)
                    base.SignedInfo.SignatureMethod = Algorithms.DsaSha1Url;
                else if (signingKey is RSA)
                    base.SignedInfo.SignatureMethod = Algorithms.RsaSha1Url;
                else
                    throw new CryptographicException("Cryptography_Xml_CreatedKeyFailed");
            }

            var signatureDescription = Algorithms.Create(Algorithms.GetSignatureAlgorithmFromUrl(this.SignedInfo.SignatureMethod));
            if (signatureDescription == null)
                throw new CryptographicException("Cryptography_Xml_SignatureDescriptionNotCreated");

            var hash = signatureDescription.CreateDigest();
            if (hash == null)
                throw new CryptographicException("Cryptography_Xml_CreateHashAlgorithmFailed");

            _ = this.GetC14NDigest(hash, prefix);
            base.m_signature.SignatureValue = signatureDescription.CreateFormatter(signingKey).CreateSignature(hash);
        }

        public XmlElement GetXml(string prefix)
        {
            var e = base.GetXml();
            SetPrefix(prefix, e);
            return e;
        }

        private byte[] GetC14NDigest(HashAlgorithm hash, string prefix)
        {
            var document = new XmlDocument
            {
                PreserveWhitespace = true
            };
            var e = base.SignedInfo.GetXml();
            _ = document.AppendChild(document.ImportNode(e, true));

            SetPrefix(prefix, document.DocumentElement);
            base.SignedInfo.CanonicalizationMethodObject.LoadInput(document);
            return base.SignedInfo.CanonicalizationMethodObject.GetDigestedOutput(hash);
        }

        private static void SetPrefix(string prefix, XmlNode node)
        {
            foreach (XmlNode n in node.ChildNodes)
                SetPrefix(prefix, n);
            node.Prefix = prefix;
        }
    }
}
