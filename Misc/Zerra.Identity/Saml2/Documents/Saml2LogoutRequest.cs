// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Documents
{
    public sealed class Saml2LogoutRequest : Saml2Document
    {
        public string ID { get; }
        public string Issuer { get; }
        public string Destination { get; }
        public string IssueInstant { get; }

        public override BindingDirection BindingDirection => BindingDirection.Request;

        public Saml2LogoutRequest(string id, string issuer, string destination)
        {
            this.ID = id;
            this.Issuer = issuer;
            this.Destination = destination;
            this.IssueInstant = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public Saml2LogoutRequest(Binding<XmlDocument> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var xmlDoc = binding.GetDocument();

            if (xmlDoc == null)
                return;

            if (xmlDoc.DocumentElement.LocalName != Saml2Names.LogoutRequest)
                throw new ArgumentException(String.Format("XmlDocument is not a SAML {0}", Saml2Names.LogoutRequest));

            this.ID = xmlDoc.DocumentElement.GetAttribute("ID");
            this.IssueInstant = xmlDoc.DocumentElement.GetAttribute("IssueInstant");
            this.Destination = xmlDoc.DocumentElement.GetAttribute("Destination");

            this.Issuer = xmlDoc.DocumentElement.GetSingleElement(null, "Issuer", false)?.InnerText;
        }

        public override XmlDocument GetSaml()
        {
            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                var xws = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false
                };

                using (var xw = XmlWriter.Create(sw, xws))
                {
                    WriteLogoutRequest(xw);
                }

                var samlString = sw.ToString();

                var document = new XmlDocument();
                document.LoadXml(samlString);
                return document;
            }
        }

        private void WriteLogoutRequest(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, "LogoutRequest", Saml2Names.ProtocolNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, Saml2Names.AssertionPrefix, null, Saml2Names.AssertionNamespace);
            xw.WriteAttributeString("ID", this.ID);
            xw.WriteAttributeString("Version", "2.0");
            xw.WriteAttributeString("IssueInstant", this.IssueInstant);
            xw.WriteAttributeString("Destination", this.Destination);

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
