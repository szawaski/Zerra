#if !NET48
using Microsoft.AspNetCore.Http;
#endif

using System.Collections.Generic;
using System.Linq;

namespace Zerra.Identity
{
    public class IdentityHttpRequest
    {
        public IReadOnlyDictionary<string, string> Query { get; private set; }
        public string QueryString { get; private set; }

        public bool HasFormContentType { get; private set; }
        public IReadOnlyDictionary<string, string> Form { get; private set; }

#if !NET48
        public IdentityHttpRequest(HttpContext context)
        {
            this.Query = context.Request.Query.ToDictionary(x => x.Key, x => (string)x.Value);
            this.QueryString = context.Request.QueryString.Value;

            this.HasFormContentType = context.Request.HasFormContentType;
            if (this.HasFormContentType)
                this.Form = context.Request.Form.ToDictionary(x => x.Key, x => (string)x.Value);
        }
#endif

    }
}
