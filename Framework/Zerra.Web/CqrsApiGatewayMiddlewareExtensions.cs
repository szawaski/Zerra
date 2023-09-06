// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;

namespace Zerra.Web
{
    public static class CqrsApiGatewayMiddlewareExtensions
    {
        public static IApplicationBuilder UseCQRSGateway(this IApplicationBuilder builder, string route = "/CQRS")
        {
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(route);
        }
    }
}