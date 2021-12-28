// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using Zerra.Identity.Saml2.Bindings;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Zerra.Identity.Saml2
{
    public abstract class Saml2Binding : Binding<XmlDocument>
    {
        public static bool IsSaml2Binding(IdentityHttpRequest request)
        {
            if (request.HasFormContentType)
                return request.Form.Keys.Contains(Saml2Names.RequestParameterName) || request.Form.Keys.Contains(Saml2Names.ResponseParameterName);
            else
                return request.Query.Keys.Contains(Saml2Names.RequestParameterName) || request.Query.Keys.Contains(Saml2Names.ResponseParameterName);
        }

        public static Saml2Binding GetBindingForRequest(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            if (request.HasFormContentType)
                return new Saml2FormBinding(request, bindingDirection);
            else
                return new Saml2QueryBinding(request, bindingDirection);
        }

        public static Saml2Binding GetBindingForResponse(WebResponse request, BindingDirection flowDirection)
        {
            return new Saml2StreamBinding(request, flowDirection);
        }

        public static Saml2Binding GetBindingForDocument(Saml2Document document, BindingType bindingType, XmlSignatureAlgorithmType? signatureAlgorithm, XmlDigestAlgorithmType? digestAlgorithm, XmlEncryptionAlgorithmType? encryptionAlgorithm)
        {
            return bindingType switch
            {
                BindingType.Null => throw new IdentityProviderException("Cannot have null binding type"),
                BindingType.Form => new Saml2FormBinding(document, signatureAlgorithm, digestAlgorithm, encryptionAlgorithm),
                BindingType.Query => new Saml2QueryBinding(document, signatureAlgorithm),
                BindingType.Stream => new Saml2StreamBinding(document),
                _ => throw new NotImplementedException(),
            };
        }

        public abstract void Sign(X509Certificate2 cert, bool requiredSignature);

        public abstract void ValidateSignature(X509Certificate2 cert, bool requiredSignature);

        public abstract void Encrypt(X509Certificate2 cert, bool requiredEncryption);

        public abstract void Decrypt(X509Certificate2 cert, bool requiredEncryption);

        public void ValidateFields(string[] expectedUrls)
        {
            var conditionNotBefore = ConditionNotBefore(this.Document.DocumentElement);
            if (conditionNotBefore.HasValue && (conditionNotBefore > DateTimeOffset.Now))
                throw new IdentityProviderException("Saml2 Document Invalid: NotBefore",
                    String.Format("Received: {0}, Expected: {1}", conditionNotBefore, DateTimeOffset.Now));

            var conditionNotOnOrAfter = ConditionNotOnOrAfter(this.Document.DocumentElement);
            if (conditionNotOnOrAfter.HasValue && (conditionNotOnOrAfter <= DateTimeOffset.Now))
                throw new IdentityProviderException("Saml2 Document Invalid: NotOnOrAfter",
                    String.Format("Received: {0}, Expected: {1}", conditionNotOnOrAfter, DateTimeOffset.Now));

            var entityDescriptorValidUntil = EntityDescriptorValidUntil(this.Document.DocumentElement);
            if (entityDescriptorValidUntil.HasValue && (entityDescriptorValidUntil <= DateTimeOffset.Now))
                throw new IdentityProviderException("Saml2 Document Invalid: ValidUntil",
                    String.Format("Received: {0}, Expected: {1}", entityDescriptorValidUntil, DateTimeOffset.Now));

            var subjectNotOnOrAfter = SubjectNotOnOrAfter(this.Document.DocumentElement);
            if (subjectNotOnOrAfter.HasValue && (subjectNotOnOrAfter <= DateTimeOffset.Now))
                throw new IdentityProviderException("Saml2 Document Invalid: NotOnOrAfter",
                    String.Format("Received: {0}, Expected: {1}", subjectNotOnOrAfter, DateTimeOffset.Now));

            if (expectedUrls != null)
            {
                var subjectRecipient = SubjectRecipient(this.Document.DocumentElement);
                if (!String.IsNullOrWhiteSpace(subjectRecipient) && !expectedUrls.Contains(subjectRecipient))
                    throw new IdentityProviderException("Saml2 Document Invalid: Recipient",
                        String.Format("Received: {0}, Expected: {1}", subjectRecipient, String.Join(", ", expectedUrls)));

                var authnRequestAssertionConsumerServiceURL = AuthnRequestAssertionConsumerServiceURL(this.Document.DocumentElement);
                if (!String.IsNullOrWhiteSpace(authnRequestAssertionConsumerServiceURL) && !expectedUrls.Contains(authnRequestAssertionConsumerServiceURL))
                    throw new IdentityProviderException("Saml2 Document Invalid: AssertionConsumerServiceURL",
                        String.Format("Received: {0}, Expected: {1}", authnRequestAssertionConsumerServiceURL, String.Join(", ", expectedUrls)));
            }
        }
        private static DateTimeOffset? ConditionNotBefore(XmlElement element)
        {
            var assertion = element?.GetSingleElement(null, "Assertion", false);
            var conditions = assertion?.GetSingleElement(null, "Conditions", false);
            var value = conditions?.GetAttribute("NotBefore");
            return !String.IsNullOrWhiteSpace(value) ? DateTimeOffset.Parse(value) : (DateTimeOffset?)null;
        }
        private static DateTimeOffset? ConditionNotOnOrAfter(XmlElement element)
        {
            var assertion = element?.GetSingleElement(null, "Assertion", false);
            var conditions = assertion?.GetSingleElement(null, "Conditions", false);
            var value = conditions?.GetAttribute("NotOnOrAfter");
            return !String.IsNullOrWhiteSpace(value) ? DateTimeOffset.Parse(value) : (DateTimeOffset?)null;
        }
        protected static DateTimeOffset? EntityDescriptorValidUntil(XmlElement element)
        {
            string value = element?.GetAttribute("validUntil");
            return !String.IsNullOrWhiteSpace(value) ? DateTimeOffset.Parse(value) : (DateTimeOffset?)null;
        }
        protected static string SubjectRecipient(XmlElement element)
        {
            var assertion = element?.GetSingleElement(null, "Assertion", false);
            var subject = assertion?.GetSingleElement(null, "Subject", false);
            var subjectConfirmation = subject?.GetSingleElement(null, "SubjectConfirmation", false);
            var subjectConfirmationData = subjectConfirmation?.GetSingleElement(null, "SubjectConfirmationData", false);
            var value = subjectConfirmationData?.GetAttribute("Recipient");
            return value;
        }
        protected static DateTimeOffset? SubjectNotOnOrAfter(XmlElement element)
        {
            var assertion = element?.GetSingleElement(null, "Assertion", false);
            var subject = assertion?.GetSingleElement(null, "Subject", false);
            var subjectConfirmation = subject?.GetSingleElement(null, "SubjectConfirmation", false);
            var subjectConfirmationData = subjectConfirmation?.GetSingleElement(null, "SubjectConfirmationData", false);
            var value = subjectConfirmationData?.GetAttribute("NotOnOrAfter");

            return !String.IsNullOrWhiteSpace(value) ? DateTimeOffset.Parse(value) : (DateTimeOffset?)null;
        }
        protected static string AuthnRequestAssertionConsumerServiceURL(XmlElement element)
        {
            string value = element?.GetAttribute("AssertionConsumerServiceURL");
            return value;
        }
    }
}
