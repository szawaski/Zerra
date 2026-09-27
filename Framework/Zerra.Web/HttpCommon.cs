// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Web
{
    internal static class HttpCommon
    {
        public const int BufferLength = 1024 * 16;

        public const string ContentTypeHeader = "Content-Type";
        public const string ProviderTypeHeader = "Provider-Type";
        //the body is the data in {int32 little endian length}{bytes} segments ended by {int32 0}, then the stream bytes to the end of the body
        public const string UploadStreamHeader = "Upload-Stream";
        public const string UploadStreamValue = "true";

        public const string ContentTypeBytes = "application/octet-stream";
        public const string ContentTypeJson = "application/json; charset=utf-8";
        public const string ContentTypeJsonNameless = "application/jsonnameless; charset=utf-8";

        public const string OriginHeader = "Origin";
        public const string VaryHeader = "Vary";
        public const string AccessControlAllowOriginHeader = "Access-Control-Allow-Origin";
        public const string AccessControlAllowMethodsHeader = "Access-Control-Allow-Methods";
        public const string AccessControlAllowHeadersHeader = "Access-Control-Allow-Headers";
    }
}