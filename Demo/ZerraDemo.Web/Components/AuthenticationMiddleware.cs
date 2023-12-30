using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ZerraDemo.Web.Components
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate requestDelegate;

        public AuthenticationMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        public Task Invoke(HttpContext context)
        {
            var claims = new Claim[]
            { 
                new(ClaimTypes.Authentication, Boolean.TrueString),
                new(ClaimTypes.NameIdentifier, "1234", ClaimValueTypes.String),
                new(ClaimTypes.Name, "Tester", ClaimValueTypes.String),
                new(ClaimTypes.Role, "Admin", ClaimValueTypes.String)
            };
            var identity = new ClaimsIdentity(claims, "HardCoded");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;
            Thread.CurrentPrincipal = principal;

            return requestDelegate(context);
        }
    }

    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
