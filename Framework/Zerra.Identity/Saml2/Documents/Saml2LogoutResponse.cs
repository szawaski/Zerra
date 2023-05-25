// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Documents
{
    public sealed class Saml2LogoutResponse : Saml2Document
    {
        public string ID { get; protected set; }
        public string Issuer { get; protected set; }
        public string Destination { get; protected set; }
        public string InResponseTo { get; protected set; }
        public BindingType BindingType { get; protected set; }
        public string IssueInstant { get; protected set; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public Saml2LogoutResponse(string id, string issuer, string destination, string inResponseTo, BindingType bindingType)
        {
            this.ID = id;
            this.Issuer = issuer;
            this.Destination = destination;
            this.InResponseTo = inResponseTo;
            this.BindingType = bindingType;
            this.IssueInstant = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public Saml2LogoutResponse(Binding<XmlDocument> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var xmlDoc = binding.GetDocument();

            if (xmlDoc == null)
                return;

            if (xmlDoc.DocumentElement.LocalName != Saml2Names.LogoutResponse)
                throw new ArgumentException(String.Format("XmlDocument is not a SAML {0}", Saml2Names.LogoutResponse));

            this.ID = xmlDoc.DocumentElement.GetAttribute("ID");
            this.IssueInstant = xmlDoc.DocumentElement.GetAttribute("IssueInstant");
            this.Destination = xmlDoc.DocumentElement.GetAttribute("Destination");
            this.InResponseTo = xmlDoc.DocumentElement.GetAttribute("InResponseTo");

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
                    WriteLogoutResponse(xw);
                }

                var samlString = sw.ToString();

                var document = new XmlDocument();
                document.LoadXml(samlString);
                return document;
            }
        }

        private void WriteLogoutResponse(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, Saml2Names.LogoutResponse, Saml2Names.ProtocolNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, Saml2Names.AssertionPrefix, null, Saml2Names.AssertionNamespace);
            xw.WriteAttributeString("ID", this.ID);
            xw.WriteAttributeString("Version", "2.0");
            xw.WriteAttributeString("IssueInstant", this.IssueInstant);
            xw.WriteAttributeString("Destination", this.Destination);
            xw.WriteAttributeString("InResponseTo", this.InResponseTo);

            WriteIssuer(xw);
            WriteStatus(xw);

            xw.WriteEndElement();
        }

        private void WriteIssuer(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Issuer", Saml2Names.AssertionNamespace);
            xw.WriteString(this.Issuer);
            xw.WriteEndElement();
        }

        private void WriteStatus(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, "Status", null);
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, "StatusCode", null);
            xw.WriteAttributeString("Value", Saml2Names.SuccessNamespace);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }
    }
}
