#if !NET48
using Microsoft.AspNetCore.Mvc;
using System.Net;
#endif

namespace Zerra.Identity
{
    public sealed class IdentityHttpResponse
    {
        public string RedirectUrl { get; private set; }
        public string ContentType { get; private set; }
        public string Content { get; private set; }

        public IdentityHttpResponse(string redirectUrl)
        {
            this.RedirectUrl = redirectUrl;
        }

        public IdentityHttpResponse(string contentType, string content)
        {
            this.ContentType = contentType;
            this.Content = content;
        }

#if !NET48
        public IActionResult ToIActionResult()
        {
            if (!System.String.IsNullOrWhiteSpace(RedirectUrl))
            {
                return new RedirectResult(RedirectUrl);
            }
            else
            {
                return new ContentResult()
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    ContentType = ContentType,
                    Content = Content
                };
            }
        }
#endif
    }
}
