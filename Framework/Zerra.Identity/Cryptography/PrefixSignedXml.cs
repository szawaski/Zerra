// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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

        public void ComputeSignature(string prefix)
        {
            MethodInfo m = typeof(SignedXml).GetMethod("BuildDigestedReferences", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(this, new object[] { });

            AsymmetricAlgorithm signingKey = base.SigningKey;
            if (signingKey == null)
            {
                throw new CryptographicException("Cryptography_Xml_LoadKeyFailed");
            }
            if (this.SignedInfo.SignatureMethod == null)
            {
                if (!(signingKey is DSA))
                {
                    if (!(signingKey is RSA))
                    {
                        throw new CryptographicException("Cryptography_Xml_CreatedKeyFailed");
                    }
                    if (this.SignedInfo.SignatureMethod == null)
                    {
                        base.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                    }
                }
                else
                {
                    base.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
                }
            }
            SignatureDescription description = Algorithms.Create(Algorithms.GetSignatureAlgorithmFromUrl(this.SignedInfo.SignatureMethod));
            if (description == null)
            {
                throw new CryptographicException("Cryptography_Xml_SignatureDescriptionNotCreated");
            }
            HashAlgorithm hash = description.CreateDigest();
            if (hash == null)
            {
                throw new CryptographicException("Cryptography_Xml_CreateHashAlgorithmFailed");
            }
            this.GetC14NDigest(hash, prefix);
            base.m_signature.SignatureValue = description.CreateFormatter(signingKey).CreateSignature(hash);
        }

        ////Origional Source
        //public void ComputeSignature()
        //{
        //    SignedXmlDebugLog.LogBeginSignatureComputation(this, m_context);

        //    BuildDigestedReferences();

        //    // Load the key
        //    AsymmetricAlgorithm key = SigningKey;

        //    if (key == null)
        //        throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_LoadKeyFailed"));

        //    // Check the signature algorithm associated with the key so that we can accordingly set the signature method
        //    if (SignedInfo.SignatureMethod == null)
        //    {
        //        if (key is DSA)
        //        {
        //            SignedInfo.SignatureMethod = XmlDsigDSAUrl;
        //        }
        //        else if (key is RSA)
        //        {
        //            // Default to RSA-SHA256 or RSA-SHA1 depending on context switch
        //            if (SignedInfo.SignatureMethod == null)
        //                SignedInfo.SignatureMethod = XmlDsigRSADefault;
        //        }
        //        else
        //        {
        //            throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreatedKeyFailed"));
        //        }
        //    }

        //    // See if there is a signature description class defined in the Config file
        //    SignatureDescription signatureDescription = Utils.CreateFromName<SignatureDescription>(SignedInfo.SignatureMethod);
        //    if (signatureDescription == null)
        //        throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureDescriptionNotCreated"));
        //    HashAlgorithm hashAlg = signatureDescription.CreateDigest();
        //    if (hashAlg == null)
        //        throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreateHashAlgorithmFailed"));
        //    byte[] hashvalue = GetC14NDigest(hashAlg);
        //    AsymmetricSignatureFormatter asymmetricSignatureFormatter = signatureDescription.CreateFormatter(key);

        //    SignedXmlDebugLog.LogSigning(this, key, signatureDescription, hashAlg, asymmetricSignatureFormatter);
        //    m_signature.SignatureValue = asymmetricSignatureFormatter.CreateSignature(hashAlg);
        //}

        public XmlElement GetXml(string prefix)
        {
            var e = base.GetXml();
            SetPrefix(prefix, e);
            return e;
        }

        private byte[] GetC14NDigest(HashAlgorithm hash, string prefix)
        {
            //string securityUrl = (this.m_containingDocument == null) ? null : this.m_containingDocument.BaseURI;
            //XmlResolver xmlResolver = new XmlSecureResolver(new XmlUrlResolver(), securityUrl);
            var document = new XmlDocument();
            document.PreserveWhitespace = true;
            var e = base.SignedInfo.GetXml();
            document.AppendChild(document.ImportNode(e, true));
            //CanonicalXmlNodeList namespaces = (this.m_context == null) ? null : Utils.GetPropagatedAttributes(this.m_context);
            //Utils.AddNamespaces(document.DocumentElement, namespaces);

            Transform canonicalizationMethodObject = base.SignedInfo.CanonicalizationMethodObject;
            //canonicalizationMethodObject.Resolver = xmlResolver;
            //canonicalizationMethodObject.BaseURI = securityUrl;
            SetPrefix(prefix, document.DocumentElement); //establecemos el prefijo antes de se que calcule el hash (o de lo contrario la firma no será válida)
            canonicalizationMethodObject.LoadInput(document);
            return canonicalizationMethodObject.GetDigestedOutput(hash);
        }

        ////Origional Source
        //private byte[] GetC14NDigest(HashAlgorithm hash)
        //{
        //    if (!bCacheValid || !this.SignedInfo.CacheValid)
        //    {
        //        string baseUri = (m_containingDocument == null ? null : m_containingDocument.BaseURI);
        //        XmlResolver resolver = (m_bResolverSet ? m_xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
        //        XmlDocument doc = Utils.PreProcessElementInput(SignedInfo.GetXml(), resolver, baseUri);

        //        // Add non default namespaces in scope
        //        CanonicalXmlNodeList namespaces = (m_context == null ? null : Utils.GetPropagatedAttributes(m_context));
        //        SignedXmlDebugLog.LogNamespacePropagation(this, namespaces);
        //        Utils.AddNamespaces(doc.DocumentElement, namespaces);

        //        Transform c14nMethodTransform = SignedInfo.CanonicalizationMethodObject;
        //        c14nMethodTransform.Resolver = resolver;
        //        c14nMethodTransform.BaseURI = baseUri;

        //        SignedXmlDebugLog.LogBeginCanonicalization(this, c14nMethodTransform);
        //        c14nMethodTransform.LoadInput(doc);
        //        SignedXmlDebugLog.LogCanonicalizedOutput(this, c14nMethodTransform);
        //        _digestedSignedInfo = c14nMethodTransform.GetDigestedOutput(hash);

        //        bCacheValid = true;
        //    }
        //    return _digestedSignedInfo;
        //}

        private static void SetPrefix(string prefix, XmlNode node)
        {
            foreach (XmlNode n in node.ChildNodes)
                SetPrefix(prefix, n);
            node.Prefix = prefix;
        }
    }
}
