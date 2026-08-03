#if !NET48
using Microsoft.AspNetCore.Mvc;
using System.Net;
#endif

namespace Zerra.Identity
{
    public sealed class IdentityHttpResponse
    {
        public string RedirectUrl { get; }
        public string ContentType { get; }
        public string Content { get; }

        public IdentityHttpResponse(string redirectUrl)
        {
            this.RedirectUrl = redirectUrl;
        }

        public IdentityHttpResponse(string contentType, string content)
        {
            this.ContentType = contentType;
            this.Content = content;
        }
    }
}
