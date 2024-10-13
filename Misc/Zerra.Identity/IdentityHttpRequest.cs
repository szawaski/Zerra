#if !NET48
using Microsoft.AspNetCore.Http;
using System.Linq;
#endif

using System.Collections.Generic;

namespace Zerra.Identity
{
    public sealed class IdentityHttpRequest
    {
        public string QueryString { get; }
        public IReadOnlyDictionary<string, string> Query { get; }

        public bool HasFormContentType { get; }
        public IReadOnlyDictionary<string, string> Form { get; }

        public IdentityHttpRequest(string queryString, Dictionary<string, string> query, bool hasFormContentType, Dictionary<string, string> form)
        {
            this.QueryString = queryString;
            this.Query = query;
            this.HasFormContentType = form is not null;
            this.HasFormContentType = hasFormContentType;
            this.Form = form;
        }

#if !NET48
        public IdentityHttpRequest(HttpContext context)
        {
            this.QueryString = context.Request.QueryString.Value;
            this.Query = context.Request.Query.ToDictionary(x => x.Key, x => (string)x.Value);

            this.HasFormContentType = context.Request.HasFormContentType;
            if (this.HasFormContentType)
                this.Form = context.Request.Form.ToDictionary(x => x.Key, x => (string)x.Value);
        }
#endif
    }
}
