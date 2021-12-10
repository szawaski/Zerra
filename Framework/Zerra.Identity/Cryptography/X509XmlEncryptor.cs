// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Linq;
using System.Xml;
using System.Text;

namespace Zerra.Identity.Cryptography
{
    public static class X509XmlEncryptor
    {
        private const bool useOeap = false;

        public static XmlDocument EncryptXmlDoc(XmlDocument xmlDoc, X509Certificate2 cert, EncryptionAlgorithm encryptionAlgorithm, string elementPrefix, string elementName, string wrapperElementPrefix = null, string wrapperElementName = null)
        {
            var rsa = cert.GetRSAPublicKey();
            if (rsa == null)
                throw new IdentityProviderException("X509 must be RSA");

            if (String.IsNullOrWhiteSpace(wrapperElementName))
                wrapperElementName = elementName;

            var element = xmlDoc.DocumentElement.GetSingleElementRequired(null, elementName, true);

            string encryptionAlgorithmUrl = Algorithms.GetEncryptionAlgorithmUrl(encryptionAlgorithm);
            string encryptionKeyAlgorithmUrl = useOeap ? Algorithms.RsaOaep : Algorithms.Rsa;

            var encryptedXml = new EncryptedXml(xmlDoc);
            var symmetricAlgorithm = Algorithms.Create(encryptionAlgorithm);

            var encryptedElementBytes = encryptedXml.EncryptData(element, symmetricAlgorithm, false);
            var encryptedKeyBytes = EncryptedXml.EncryptKey(symmetricAlgorithm.Key, rsa, useOeap);

            var encryptedKey = new EncryptedKey();
            encryptedKey.EncryptionMethod = new EncryptionMethod(encryptionKeyAlgorithmUrl);
            encryptedKey.CipherData = new CipherData(encryptedKeyBytes);

            EncryptedData encryptedDataElement = new EncryptedData();
            encryptedDataElement.Type = EncryptedXml.XmlEncElementUrl;
            encryptedDataElement.EncryptionMethod = new EncryptionMethod(encryptionAlgorithmUrl);
            encryptedDataElement.CipherData = new CipherData(encryptedElementBytes);
            encryptedDataElement.KeyInfo = new KeyInfo();
            encryptedDataElement.KeyInfo.AddClause(new KeyInfoX509Data(cert));
            encryptedDataElement.KeyInfo.AddClause(new KeyInfoEncryptedKey(encryptedKey));

            EncryptedXml.ReplaceElement(element, encryptedDataElement, true);

            XmlHelper.SetPrefix("xenc", element.ChildNodes[0]);

            if (wrapperElementName != elementName)
            {
                xmlDoc.DocumentElement.RemoveChild(element);

                var newElement = xmlDoc.CreateElement(wrapperElementPrefix, wrapperElementName, element.NamespaceURI);
                xmlDoc.DocumentElement.AppendChild(newElement);

                newElement.AppendChild(element.ChildNodes[0]);
            }

            return xmlDoc;
        }

        public static XmlDocument DecryptXmlDoc(XmlDocument xmlDoc, X509Certificate2 cert)
        {
            var rsa = cert.GetRSAPrivateKey();
            if (rsa == null)
                throw new IdentityProviderException("X509 must be RSA");

            var elements = xmlDoc.DocumentElement.GetElements(null, "EncryptedData", true).Select(x => x.ParentNode).OfType<XmlElement>().ToArray();

            foreach (var element in elements)
            {
                var encryptedXml = new EncryptedXml(xmlDoc);
                var encryptedDataElement = new EncryptedData();
                encryptedDataElement.LoadXml((XmlElement)element.ChildNodes[0]);

                var encryptedKeyInfo = encryptedDataElement.KeyInfo.OfType<KeyInfoEncryptedKey>().First();
                var encryptionAlgorithm = GetEncryptionAlgorithm(xmlDoc.DocumentElement);
                var symmetricAlgorithm = Algorithms.Create(encryptionAlgorithm);
                symmetricAlgorithm.Key = EncryptedXml.DecryptKey(encryptedKeyInfo.EncryptedKey.CipherData.CipherValue, rsa, useOeap);

                var decryptedBytes = encryptedXml.DecryptData(encryptedDataElement, symmetricAlgorithm);
                element.ParentNode.InnerXml = Encoding.UTF8.GetString(decryptedBytes);
            }

            return xmlDoc;
        }

        public static bool HasEncryptedDataElements(XmlElement element)
        {
            return element.GetElements(null, "EncryptedData", true).Count > 0;
        }

        public static EncryptionAlgorithm GetEncryptionAlgorithm(XmlElement element)
        {
            EncryptionAlgorithm? encryptionAlgorithm = null;

            var signatureElements = element.GetElements(null, "EncryptedData", true);

            foreach (var signatureElement in signatureElements)
            {
                var encryptionMethod = signatureElement.GetSingleElementRequired(null, "EncryptionMethod", false);
                var encryptionAlgorithmUrl = encryptionMethod.GetAttributeRequired("Algorithm");
                var thisEncryptionAlgorithm = Algorithms.GetEncryptionAlgorithmFromUrl(encryptionAlgorithmUrl);
                if (encryptionAlgorithm.HasValue)
                {
                    if (encryptionAlgorithm.Value != thisEncryptionAlgorithm)
                        throw new IdentityProviderException("More than one type of encyption algorithm found");
                }
                else
                {
                    encryptionAlgorithm = thisEncryptionAlgorithm;
                }
            }
            if (encryptionAlgorithm == null)
                throw new IdentityProviderException("Encryption algorithm not found");
            return encryptionAlgorithm.Value;
        }
    }
}
