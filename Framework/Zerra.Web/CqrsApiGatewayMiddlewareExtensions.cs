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
    }
}