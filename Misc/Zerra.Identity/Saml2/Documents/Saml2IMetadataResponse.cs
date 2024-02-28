// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace Zerra.Identity.Saml2.Documents
{
    public sealed class Saml2IMetadataResponse : Saml2Document
    {
        public string EntityID { get; }
        public string ValidUntil { get; }
        public string LoginUrl { get; }
        public string LogoutUrl { get; }
        public bool WantAuthnRequestsSigned { get; }

        public string OrganizationName { get; }
        public string OrganizationDisplayName { get; }
        public string OrganizationUrl { get; }

        public string TechnicalContactName { get; }
        public string TechnicalContactEmail { get; }

        public string SupportContactName { get; }
        public string SupportContactEmail { get; }

        public X509Certificate2 IdentityProviderCert { get; }
        public X509Certificate2 ServiceProviderCert { get; }

        public static readonly string[] NameIDFormats = new string[]
        {
            Saml2Names.NameIDFormatUnspecifiedNamespace,
            Saml2Names.NameIDFormatEmailAddressNamespace
        };

        public override BindingDirection BindingDirection => BindingDirection.Response;

        public Saml2IMetadataResponse(
            string entityID,
            string loginUrl,
            string logoutUrl,
            bool wantSigning,
            X509Certificate2 identityProviderCert,
            X509Certificate2 serviceProviderCert,
            string organizationName,
            string organizationDisplayName,
            string organizationUrl,
            string technicalContactName,
            string technicalContactEmail,
            string supportContactName,
            string supportContactEmail
            )
        {
            this.EntityID = entityID;
            //this.ValidUntil = DateTimeOffset.UtcNow.AddYears(10).ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.LoginUrl = loginUrl;
            this.LogoutUrl = logoutUrl;
            this.WantAuthnRequestsSigned = wantSigning;
            this.IdentityProviderCert = identityProviderCert;
            this.ServiceProviderCert = serviceProviderCert;
            this.OrganizationName = organizationName;
            this.OrganizationDisplayName = organizationDisplayName;
            this.OrganizationUrl = organizationUrl;
            this.TechnicalContactName = technicalContactName;
            this.TechnicalContactEmail = technicalContactEmail;
            this.SupportContactName = supportContactName;
            this.SupportContactEmail = supportContactEmail;
        }

        public Saml2IMetadataResponse(Binding<XmlDocument> binding)
        {
            if (binding.BindingDirection != this.BindingDirection)
                throw new ArgumentException("Binding has the wrong binding direction for this document");

            var xmlDoc = binding.GetDocument();

            if (xmlDoc == null)
                return;

            throw new NotImplementedException("Need to parse SAML to load the document variables");
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
                    WriteEntityDescriptor(xw);
                }

                var samlString = sw.ToString();

                var document = new XmlDocument();
                document.LoadXml(samlString);
                return document;
            }
        }


        private void WriteEntityDescriptor(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.MetadataPrefix, Saml2Names.Metadata, Saml2Names.MetadataNamespace);
            //xw.WriteAttributeString("validUntil", this.ValidUntil);
            //xw.WriteAttributeString("cacheDuration", "PT604800S");
            xw.WriteAttributeString("entityID", this.EntityID);

            WriteIDPSSODescriptor(xw);
            WriteOrganization(xw);
            WriteTechnicalContact(xw);
            WriteSupportContact(xw);

            xw.WriteEndElement();
        }

        private void WriteIDPSSODescriptor(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.MetadataPrefix, Saml2Names.IdentityProviderMetadata, null);
            xw.WriteAttributeString("WantAuthnRequestsSigned", this.WantAuthnRequestsSigned.ToString().ToLower());
            xw.WriteAttributeString("protocolSupportEnumeration", Saml2Names.ProtocolNamespace);

            WriteSigningKey(xw);
            WriteEncryptionKey(xw);

            Saml2IMetadataResponse.WriteNameIDFormat(xw);
            WriteSingleSignOnService(xw);
            WriteSingleLogoutService(xw);

            xw.WriteEndElement();
        }

        private void WriteSigningKey(XmlWriter xw)
        {
            if (this.IdentityProviderCert != null)
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "KeyDescriptor", null);
                xw.WriteAttributeString("use", "signing");

                xw.WriteStartElement("ds", "KeyInfo", "http://www.w3.org/2000/09/xmldsig#");
                xw.WriteStartElement("ds", "X509Data", null);
                xw.WriteStartElement("ds", "X509Certificate", null);
                var certBytes = this.IdentityProviderCert.Export(X509ContentType.Cert);
                var certString = Convert.ToBase64String(certBytes);
                xw.WriteString(certString);
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndElement();

                xw.WriteEndElement();
            }
        }

        private void WriteEncryptionKey(XmlWriter xw)
        {
            if (this.ServiceProviderCert != null)
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "KeyDescriptor", null);
                xw.WriteAttributeString("use", "encryption");

                xw.WriteStartElement("ds", "KeyInfo", "http://www.w3.org/2000/09/xmldsig#");
                xw.WriteStartElement("ds", "X509Data", null);
                xw.WriteStartElement("ds", "X509Certificate", null);
                var certBytes = this.ServiceProviderCert.Export(X509ContentType.Cert);
                var certString = Convert.ToBase64String(certBytes);
                xw.WriteString(certString);
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndElement();

                xw.WriteEndElement();
            }
        }

        private void WriteSingleLogoutService(XmlWriter xw)
        {
            if (!String.IsNullOrWhiteSpace(this.LogoutUrl))
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "SingleLogoutService", null);
                xw.WriteAttributeString("Binding", Saml2Names.RedirectBindingNamespace);
                xw.WriteAttributeString("Location", this.LogoutUrl);
                xw.WriteEndElement();
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "SingleLogoutService", null);
                xw.WriteAttributeString("Binding", Saml2Names.PostBindingNamespace);
                xw.WriteAttributeString("Location", this.LogoutUrl);
                xw.WriteEndElement();
            }
        }

        private void WriteSingleSignOnService(XmlWriter xw)
        {
            xw.WriteStartElement(Saml2Names.MetadataPrefix, "SingleSignOnService", null);
            xw.WriteAttributeString("Binding", Saml2Names.RedirectBindingNamespace);
            xw.WriteAttributeString("Location", this.LoginUrl);
            xw.WriteEndElement();
            xw.WriteStartElement(Saml2Names.MetadataPrefix, "SingleSignOnService", null);
            xw.WriteAttributeString("Binding", Saml2Names.PostBindingNamespace);
            xw.WriteAttributeString("Location", this.LoginUrl);
            xw.WriteEndElement();
        }

        private static void WriteNameIDFormat(XmlWriter xw)
        {
            foreach (var nameIDFormat in NameIDFormats)
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "NameIDFormat", null);
                xw.WriteString(nameIDFormat);
                xw.WriteEndElement();
            }
        }

        private void WriteOrganization(XmlWriter xw)
        {
            if (!String.IsNullOrWhiteSpace(this.OrganizationName) || !String.IsNullOrWhiteSpace(this.OrganizationDisplayName) || !String.IsNullOrWhiteSpace(this.OrganizationUrl))
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "Organization", null);

                if (!String.IsNullOrWhiteSpace(this.OrganizationName))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "OrganizationName", null);
                    xw.WriteAttributeString("xml", "lang", null, "en-US");
                    xw.WriteString(this.OrganizationName);
                    xw.WriteEndElement();
                }

                if (!String.IsNullOrWhiteSpace(this.OrganizationDisplayName))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "OrganizationDisplayName", null);
                    xw.WriteAttributeString("xml", "lang", null, "en-US");
                    xw.WriteString(this.OrganizationDisplayName);
                    xw.WriteEndElement();
                }

                if (!String.IsNullOrWhiteSpace(this.OrganizationUrl))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "OrganizationURL", null);
                    xw.WriteAttributeString("xml", "lang", null, "en-US");
                    xw.WriteString(this.OrganizationUrl);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }
        }

        private void WriteTechnicalContact(XmlWriter xw)
        {
            if (!String.IsNullOrWhiteSpace(this.TechnicalContactName) || !String.IsNullOrWhiteSpace(this.TechnicalContactEmail))
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "ContactPerson", null);
                xw.WriteAttributeString("contactType", "technical");

                if (!String.IsNullOrWhiteSpace(this.TechnicalContactName))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "GivenName", null);
                    xw.WriteString(this.TechnicalContactName);
                    xw.WriteEndElement();
                }

                if (!String.IsNullOrWhiteSpace(this.TechnicalContactEmail))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "EmailAddress", null);
                    xw.WriteString(this.TechnicalContactEmail);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }
        }

        private void WriteSupportContact(XmlWriter xw)
        {
            if (!String.IsNullOrWhiteSpace(this.SupportContactName) || !String.IsNullOrWhiteSpace(this.SupportContactEmail))
            {
                xw.WriteStartElement(Saml2Names.MetadataPrefix, "ContactPerson", null);
                xw.WriteAttributeString("contactType", "support");

                if (!String.IsNullOrWhiteSpace(this.SupportContactName))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "GivenName", null);
                    xw.WriteString(this.SupportContactName);
                    xw.WriteEndElement();
                }

                if (!String.IsNullOrWhiteSpace(this.SupportContactEmail))
                {
                    xw.WriteStartElement(Saml2Names.MetadataPrefix, "EmailAddress", null);
                    xw.WriteString(this.SupportContactEmail);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }
        }
    }
}
