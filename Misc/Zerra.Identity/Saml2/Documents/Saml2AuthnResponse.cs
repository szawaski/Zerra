// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Documents
{
    public sealed class Saml2AuthnResponse : Saml2Document
    {
        public string ID { get; }
        public string Issuer { get; }
        public string Audience { get; }
        public string DestinationUrl { get; }
        public string InResponseTo { get; }
        public string UserID { get; }
        public string UserName { get; }
        public string[] Roles { get; }

        public string IssueInstant { get; }
        public string NotBefore { get; }
        public string NotOnOrAfter { get; }
        public string AssertionID { get; }
        public string SubjectNameID { get; }
        public string SessionIndex { get; }

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public Saml2AuthnResponse(string id, string issuer, string audience, string destinationUrl, string inResponseTo, IdentityModel identity)
        {
            this.ID = id;
            this.Issuer = issuer;
            this.Audience = audience;
            this.DestinationUrl = destinationUrl;
            this.InResponseTo = inResponseTo;
            this.UserID = identity.UserID;
            this.UserName = identity.UserName;
            this.Roles = identity.Roles;
            this.IssueInstant = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.NotBefore = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.NotOnOrAfter = DateTimeOffset.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.AssertionID = "_" + System.Guid.NewGuid().ToString();
            this.SubjectNameID = "_" + System.Guid.NewGuid().ToString();
            this.SessionIndex = "_" + System.Guid.NewGuid().ToString();
        }

        public Saml2AuthnResponse(Binding<XmlDocument> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var xmlDoc = binding.GetDocument();

            if (xmlDoc == null)
                return;

            if (xmlDoc.DocumentElement.LocalName != Saml2Names.AuthnResponse)
                throw new ArgumentException(String.Format("XmlDocument is not a SAML {0}", Saml2Names.AuthnResponse));

            this.ID = xmlDoc.DocumentElement.GetAttribute("ID");
            this.IssueInstant = xmlDoc.DocumentElement.GetAttribute("IssueInstant");
            this.DestinationUrl = xmlDoc.DocumentElement.GetAttribute("Destination");
            this.InResponseTo = xmlDoc.DocumentElement.GetAttribute("InResponseTo");

            this.Issuer = xmlDoc.DocumentElement.GetSingleElement(null, "Issuer", false)?.InnerText;

            var assertion = xmlDoc.DocumentElement.GetSingleElement(null, "Assertion", false);
            this.AssertionID = assertion?.GetAttribute("ID");

            var conditions = assertion?.GetSingleElement(null, "Conditions", false);
            this.NotBefore = conditions?.GetAttribute("NotBefore");
            this.NotOnOrAfter = conditions?.GetAttribute("NotOnOrAfter");

            var audienceRestriction = conditions?.GetSingleElement(null, "AudienceRestriction", false);
            var audience = audienceRestriction?.GetSingleElement( null, "Audience", false);
            this.Audience = audience?.InnerText;

            var subject = assertion?.GetSingleElement( null, "Subject", false);
            this.SubjectNameID = subject?.GetSingleElement(null, "NameID", false)?.InnerText;

            var authnStatement = assertion?.GetSingleElement(null, "AuthnStatement", false);
            this.SessionIndex = authnStatement?.GetAttribute("SessionIndex");

            var attributeStatement = assertion?.GetSingleElement(null, "AttributeStatement", false);
            var attributes = attributeStatement?.GetElements(null, "Attribute", false);

            var roles = new List<string>();
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    var name = attribute.GetAttribute("Name").ToLower();
                    var value = attribute.GetSingleElement(null, "AttributeValue", false)?.InnerText;

                    switch (name)
                    {
                        case "uid": this.UserID = value; break;
                        case "username": this.UserName = value; break;
                        case "role": roles.Add(value); break;
                    }
                }
            }
            this.Roles = roles.ToArray();
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
                    WriteResponse(xw);
                }

                var samlString = sw.ToString();

                var document = new XmlDocument();
                document.LoadXml(samlString);
                return document;
            }
        }

        private void WriteResponse(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, Saml2Names.AuthnResponse, Saml2Names.ProtocolNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, Saml2Names.AssertionPrefix, null, Saml2Names.AssertionNamespace);
            xw.WriteAttributeString("ID", this.ID);
            xw.WriteAttributeString("Version", "2.0");
            xw.WriteAttributeString("IssueInstant", this.IssueInstant);
            xw.WriteAttributeString("Destination", this.DestinationUrl);
            xw.WriteAttributeString("InResponseTo", this.InResponseTo);

            WriteIssuer(xw);
            WriteStatus(xw);
            WriteAssertation(xw);

            xw.WriteEndElement();
        }

        private void WriteAssertation(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Assertion", Saml2Names.AssertionNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, "xsi", null, Saml2Names.XmlSchemaInstanceNamespace);
            xw.WriteAttributeString(Saml2Names.NamespacePrefix, "xs", null, Saml2Names.XmlSchemaNamespace);
            xw.WriteAttributeString("ID", this.AssertionID);
            xw.WriteAttributeString("Version", "2.0");
            xw.WriteAttributeString("IssueInstant", this.IssueInstant);

            WriteIssuer(xw);
            WriteSubject(xw);
            WriteConditions(xw);
            WriteAuthnStatement(xw);
            WriteAttributes(xw);

            xw.WriteEndElement();
        }

        private void WriteIssuer(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Issuer", Saml2Names.AssertionNamespace);
            xw.WriteString(this.Issuer);
            xw.WriteEndElement();
        }

        private static void WriteStatus(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, "Status", null);
            xw.WriteStartElement(Saml2Names.ProtocolPrefix, "StatusCode", null);
            xw.WriteAttributeString("Value", Saml2Names.SuccessNamespace);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private void WriteSubject(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Subject", null);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "NameID", null);
            //xw.WriteAttributeString("SPNameQualifier", "http://sp.example.com/demo1/metadata.php");
            xw.WriteAttributeString("Format", Saml2Names.NameIDFormatUnspecifiedNamespace);
            xw.WriteString(this.SubjectNameID);
            xw.WriteEndElement();
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "SubjectConfirmation", null);
            xw.WriteAttributeString("Method", Saml2Names.BearerNamespace);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "SubjectConfirmationData", null);
            xw.WriteAttributeString("NotOnOrAfter", this.NotOnOrAfter);
            xw.WriteAttributeString("Recipient", this.DestinationUrl);
            xw.WriteAttributeString("InResponseTo", this.InResponseTo);
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private void WriteConditions(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Conditions", null);
            xw.WriteAttributeString("NotBefore", this.NotBefore);
            xw.WriteAttributeString("NotOnOrAfter", this.NotOnOrAfter);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AudienceRestriction", null);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Audience", null);
            xw.WriteString(this.Audience);
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private void WriteAuthnStatement(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AuthnStatement", null);
            xw.WriteAttributeString("AuthnInstant", this.IssueInstant);
            xw.WriteAttributeString("SessionIndex", this.SessionIndex);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AuthnContext", null);
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AuthnContextClassRef", null);
            xw.WriteString(Saml2Names.PasswordNamespace);
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private void WriteAttributes(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AttributeStatement", null);

            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Attribute", null);
            xw.WriteAttributeString("Name", "uid");
            xw.WriteAttributeString("NameFormat", "");
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AttributeValue", null);
            xw.WriteAttributeString("xsi", "type", null, "xs:string");
            xw.WriteString(this.UserID);
            xw.WriteEndElement();
            xw.WriteEndElement();

            xw.WriteStartElement(Saml2Names.AssertionPrefix, "Attribute", null);
            xw.WriteAttributeString("Name", "username");
            xw.WriteAttributeString("NameFormat", "");
            xw.WriteStartElement(Saml2Names.AssertionPrefix, "AttributeValue", null);
            xw.WriteAttributeString("xsi", "type", null, "xs:string");
            xw.WriteString(this.UserName);
            xw.WriteEndElement();
            xw.WriteEndElement();

            foreach (var role in this.Roles)
            {
                xw.WriteStartElement(Saml2Names.AssertionPrefix, "Attribute", null);
                xw.WriteAttributeString("Name", "role");
                xw.WriteAttributeString("NameFormat", "");
                xw.WriteStartElement(Saml2Names.AssertionPrefix, "AttributeValue", null);
                xw.WriteAttributeString("xsi", "type", null, "xs:string");
                xw.WriteString(role);
                xw.WriteEndElement();
                xw.WriteEndElement();
            }

            xw.WriteEndElement();
        }
    }
}
