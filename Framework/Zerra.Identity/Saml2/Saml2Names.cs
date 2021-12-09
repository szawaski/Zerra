// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.Saml2
{
    public static class Saml2Names
    {
        public const string RequestParameterName = "SAMLRequest";
        public const string ResponseParameterName = "SAMLResponse";
        public const string RelayStateParameterName = "RelayState";
        public const string SignatureAlgorithmParameterName = "SigAlg";
        public const string SignatureParameterName = "Signature";

        public const string NamespacePrefix = "xmlns";
        public const string AssertionPrefix = "saml";
        public const string ProtocolPrefix = "samlp";
        public const string MetadataPrefix = "md";

        public const string AuthnRequest = "AuthnRequest";
        public const string AuthnResponse = "Response";
        public const string LogoutRequest = "LogoutRequest";
        public const string LogoutResponse = "LogoutResponse";
        public const string Metadata = "EntityDescriptor";
        public const string IdentityProviderMetadata = "IDPSSODescriptor";

        public const string AssertionNamespace = "urn:oasis:names:tc:SAML:2.0:assertion";
        public const string ProtocolNamespace = "urn:oasis:names:tc:SAML:2.0:protocol";
        public const string MetadataNamespace = "urn:oasis:names:tc:SAML:2.0:metadata";
        public const string RedirectBindingNamespace = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect";
        public const string PostBindingNamespace = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";
        public const string SuccessNamespace = "urn:oasis:names:tc:SAML:2.0:status:Success";
        public const string BearerNamespace = "urn:oasis:names:tc:SAML:2.0:cm:bearer";
        public const string PasswordNamespace = "urn:oasis:names:tc:SAML:2.0:ac:classes:Password";
        public const string NameIDTransientNamespace = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";
        public const string NameIDFormatUnspecifiedNamespace = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";
        public const string NameIDFormatEmailAddressNamespace = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
        public const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    }
}
