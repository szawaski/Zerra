// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Documents
{
    public class Saml2AuthnRequest : Saml2Document
    {
        public string ID { get; protected set; }
        public string Issuer { get; protected set; }
        public string AssertionConsumerServiceURL { get; protected set; }
        public BindingType BindingType { get; protected set; }
        public string IssueInstant { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public Saml2AuthnRequest(string id, string issuer, string assertionConsumerServiceURL, BindingType bindingType)
        {
            this.ID = id;
            this.Issuer = issuer;
            this.AssertionConsumerServiceURL = assertionConsumerServiceURL;
            this.BindingType = bindingType;
            this.IssueInstant = DateTimeOffset.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public Saml2AuthnRequest(Binding<XmlDocument> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var xmlDoc = binding.GetDocument();

            if (xmlDoc == null)
                return;

            if (xmlDoc.DocumentElement.LocalName != Saml2Names.AuthnRequest)
                throw new ArgumentException(String.Format("XmlDocument is not a SAML {0}", Saml2Names.AuthnRequest));

            this.ID = xmlDoc.DocumentElement.GetAttribute("ID");
            this.IssueInstant = xmlDoc.DocumentElement.GetAttribute("IssueInstant");
            this.AssertionConsumerServiceURL = xmlDoc.DocumentElement.GetAttribute("AssertionConsumerServiceURL");
            var protocolBinding = xmlDoc.DocumentElement.GetAttribute("ProtocolBinding");

            this.BindingType = protocolBinding switch
            {
                Saml2Names.PostBindingNamespace => BindingType.Form,
                Saml2Names.RedirectBindingNamespace => BindingType.Query,
                _ => throw new IdentityProviderException("Binding Type is not supported",
String.Format("Received: {0}", protocolBinding)),
            };
            this.Issuer = xmlDoc.DocumentElement.GetSingleElement(null, "Issuer", true)?.InnerText;
        }

        public override XmlDocument GetSaml()
        {
            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                var xws = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false
                };

                using (XmlWriter xw = XmlWriter.Create(sw, xws))
                {
                    WriteAuthnRequest(xw);
                }

                var samlString = sw.ToString();

                var document = new XmlDocument();
                document.LoadXml(samlString);
                return document;
            }
        }

        private void WriteAuthnRequest(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, Saml2Names.AuthnRequest, Saml2Names.ProtocolNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, Saml2Names.AssertionPrefix, null, Saml2Names.AssertionNamespace);
            xw.WriteAttributeString("ID", this.ID);
            xw.WriteAttributeString("Version", "2.0");
            xw.WriteAttributeString("IssueInstant", this.IssueInstant);

            string protocolBinding = null;
            protocolBinding = this.BindingType switch
            {
                BindingType.Form => Saml2Names.PostBindingNamespace,
                BindingType.Query => Saml2Names.RedirectBindingNamespace,
                _ => throw new IdentityProviderException("Binding Type is not supported",
String.Format("Received: {0}", protocolBinding)),
            };
            xw.WriteAttributeString("ProtocolBinding", protocolBinding);

            xw.WriteAttributeString("AssertionConsumerServiceURL", this.AssertionConsumerServiceURL);

            WriteIssuer(xw);

            xw.WriteEndElement();
        }

        private void WriteIssuer(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Issuer", Saml2Names.AssertionNamespace);
            xw.WriteString(this.Issuer);
            xw.WriteEndElement();
        }
    }
}
