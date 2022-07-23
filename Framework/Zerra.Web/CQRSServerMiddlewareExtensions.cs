// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    public static class CQRSServerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCQRSServer(this IApplicationBuilder builder, string route = "/CQRS", NetworkType networkType = NetworkType.Api, ContentType? contentType = null, IHttpAuthorizer apiAuthorizer = null, string[] allowOrigins = null)
        {
            return builder.UseMiddleware<CQRSServerMiddleware>(route, networkType, contentType, apiAuthorizer, allowOrigins);
        }
    }
}