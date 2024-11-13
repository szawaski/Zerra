// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Web
{
    public static class HttpCommon
    {
        public const int BufferLength = 1024 * 16;

        public const string ContentTypeHeader = "Content-Type";
        public const string ProviderTypeHeader = "Provider-Type";

        public const string ContentTypeBytes = "application/octet-stream";
        public const string ContentTypeJson = "application/json; charset=utf-8";
        public const string ContentTypeJsonNameless = "application/jsonnameless; charset=utf-8";

        public const string OriginHeader = "Origin";
        public const string AccessControlAllowOriginHeader = "Access-Control-Allow-Origin";
        public const string AccessControlAllowMethodsHeader = "Access-Control-Allow-Methods";
        public const string AccessControlAllowHeadersHeader = "Access-Control-Allow-Headers";
    }
}