// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;

namespace Zerra.Web
{
    public static class CQRSGatewayMiddlewareMiddlewareExtensions
    {
        public static IApplicationBuilder UseCQRSGateway(this IApplicationBuilder builder, string route = "/CQRS")
        {
            return builder.UseMiddleware<CQRSGatewayMiddleware>(route);
        }
    }
}