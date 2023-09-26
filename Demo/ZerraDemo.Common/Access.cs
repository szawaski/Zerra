using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;

namespace ZerraDemo.Common
{
    public static class Access
    {
        public static void Authorize()
        {
            if (Thread.CurrentPrincipal is not ClaimsPrincipal principal)
                throw new SecurityException();
            if (!principal.Identity.IsAuthenticated)
                throw new SecurityException();
        }

        public static string GetUserName()
        {
            if (Thread.CurrentPrincipal is not ClaimsPrincipal principal)
                throw new SecurityException();
            if (!principal.Identity.IsAuthenticated)
                throw new SecurityException();
            var token = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            if (token == null)
                throw new SecurityException();
            return token;
        }

        public static void CheckRole(params string[] roles)
        {
            if (!IsInRole(roles))
                throw new SecurityException();
        }
        public static bool IsInRole(params string[] roles)
        {
            if (Thread.CurrentPrincipal is not ClaimsPrincipal principal)
                return false;
            if (!principal.Identity.IsAuthenticated)
                return false;
            foreach (var role in roles)
            {
                var token = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
                if (token != null && token == role)
                    return true;
            }
            return false;
        }
    }
}
