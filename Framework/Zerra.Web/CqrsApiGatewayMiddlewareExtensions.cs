// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    public static class CqrsApiGatewayMiddlewareExtensions
    {
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, string? route = "/CQRS")
        {
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(route);
        }
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, ICqrsAuthorizer authorizer, string? route = "/CQRS")
        {
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(authorizer, route);
        }

        //allowOrigins are CORS origins allowed to call the gateway as scheme://host[:port] or host, null, empty, or "*" allows all
        //the middleware is created directly because UseMiddleware can't match a null argument to a constructor parameter
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, string? route, string[]? allowOrigins)
        {
            return builder.Use(next => new CqrsApiGatewayMiddleware(next, null, route, allowOrigins).Invoke);
        }
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, ICqrsAuthorizer? authorizer, string? route, string[]? allowOrigins)
        {
            return builder.Use(next => new CqrsApiGatewayMiddleware(next, authorizer, route, allowOrigins).Invoke);
        }
    }
}