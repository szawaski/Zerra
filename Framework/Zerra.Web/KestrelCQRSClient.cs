// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Zerra.Logging;
using System.Collections.Generic;
using System.Net.Http;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using System.Net.Http.Headers;
using Zerra.Encryption;
using Zerra.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Linq;

namespace Zerra.Web
{
    public sealed class KestrelCQRSClient : CQRSClientBase, IDisposable
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig symmetricConfig;
        private readonly ICQRSAuthorizer authorizer;
        private readonly HttpClient client;

        public KestrelCQRSClient(ContentType contentType, string serviceUrl, SymmetricConfig symmetricConfig, ICQRSAuthorizer authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            this.client = new HttpClient();

            _ = Log.InfoAsync($"{nameof(CQRS.Network.HttpCQRSClient)} Started For {this.contentType} {this.serviceUrl}");
        }

        protected override TReturn CallInternal<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CQRSRequestData()
            {
                ProviderType = interfaceType.Name,
                ProviderMethod = methodName,

                Claims = claims,
                Source = source
            };
            data.AddProviderArguments(arguments);

            IDictionary<string, IList<string>> authHeaders = null;
            if (authorizer != null)
                authHeaders = authorizer.BuildAuthHeaders();

            var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            HttpResponseMessage response = null;
            Stream responseBodyStream = null;
            try
            {
                request.Content = new WriteStreamContent((requestBodyStream) =>
                {
                    if (symmetricConfig != null)
                    {
                        FinalBlockStream requestBodyCryptoStream = null;
                        try
                        {
                            requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                            ContentTypeSerializer.Serialize(contentType, requestBodyCryptoStream, data);
                            requestBodyCryptoStream.FlushFinalBlock();
                        }
                        finally
                        {
                            requestBodyCryptoStream?.Dispose();
                        }
                    }
                    else
                    {
                        ContentTypeSerializer.Serialize(contentType, requestBodyStream, data);
                        requestBodyStream.Flush();
                    }
                });

                request.Content.Headers.ContentType = contentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };
                request.Headers.Add(HttpCommon.ProviderTypeHeader, data.ProviderType);
                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                request.Headers.Host = serviceUrl.Authority;
                request.Headers.Add(HttpCommon.OriginHeader, serviceUrl.Host);

                if (authHeaders != null)
                {
                    foreach (var authHeader in authHeaders)
                    {
                        foreach (var authHeaderValue in authHeader.Value)
                        {
                            request.Headers.Add(authHeader.Key, authHeaderValue);
                        }
                    }
                }

                response = client.SendAsync(request).GetAwaiter().GetResult();
                responseBodyStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = ContentTypeSerializer.DeserializeException(contentType, responseBodyStream);
                    throw responseException;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                }
                else
                {
                    var model = ContentTypeSerializer.Deserialize<TReturn>(contentType, responseBodyStream);
                    responseBodyStream.Dispose();
                    response.Dispose();
                    return model;
                }
            }
            catch
            {
                if (responseBodyStream != null)
                    responseBodyStream.Dispose();
                if (response != null)
                    response.Dispose();
                throw;
            }
        }

        protected override async Task<TReturn> CallInternalAsync<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CQRSRequestData()
            {
                ProviderType = interfaceType.Name,
                ProviderMethod = methodName,

                Claims = claims,
                Source = source
            };
            data.AddProviderArguments(arguments);

            IDictionary<string, IList<string>> authHeaders = null;
            if (authorizer != null)
                authHeaders = authorizer.BuildAuthHeaders();

            var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            HttpResponseMessage response = null;
            Stream responseBodyStream = null;
            try
            {
                request.Content = new WriteStreamContent(async (requestBodyStream) =>
                {
                    if (symmetricConfig != null)
                    {
                        FinalBlockStream requestBodyCryptoStream = null;
                        try
                        {
                            requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                            await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
#if NET5_0_OR_GREATER
                            await requestBodyCryptoStream.FlushFinalBlockAsync();
#else
                            requestBodyCryptoStream.FlushFinalBlock();
#endif
                        }
                        finally
                        {
                            if (requestBodyCryptoStream != null)
                            {
#if NETSTANDARD2_0
                                requestBodyCryptoStream.Dispose();
#else
                                await requestBodyCryptoStream.DisposeAsync();
#endif
                            }
                        }
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                        await requestBodyStream.FlushAsync();
                    }
                });

                request.Headers.Add(HttpCommon.ProviderTypeHeader, data.ProviderType);
                request.Content.Headers.ContentType = contentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };
                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                request.Headers.Host = serviceUrl.Authority;
                request.Headers.Add(HttpCommon.OriginHeader, serviceUrl.Host);

                if (authHeaders != null)
                {
                    foreach (var authHeader in authHeaders)
                    {
                        foreach (var authHeaderValue in authHeader.Value)
                            request.Headers.Add(authHeader.Key, authHeaderValue);
                    }
                }

                response = await client.SendAsync(request);
                responseBodyStream = await response.Content.ReadAsStreamAsync();

                if (symmetricConfig != null)
                    responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                    throw responseException;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                }
                else
                {
                    var model = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseBodyStream);
#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                    response.Dispose();
                    return model;
                }
            }
            catch
            {
                if (responseBodyStream != null)
                {
#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                }
                if (response != null)
                    response.Dispose();
                throw;
            }
        }

        protected override async Task DispatchInternal(ICommand command, bool messageAwait, string source)
        {
            var messageType = command.GetType();
            var messageTypeName = messageType.GetNiceName();

            var messageData = JsonSerializer.Serialize(command, messageType);

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CQRSRequestData()
            {
                MessageType = messageTypeName,
                MessageData = messageData,
                MessageAwait = messageAwait,

                Claims = claims,
                Source = source
            };

            IDictionary<string, IList<string>> authHeaders = null;
            if (authorizer != null)
                authHeaders = authorizer.BuildAuthHeaders();

            var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            HttpResponseMessage response = null;
            Stream responseBodyStream = null;
            try
            {
                request.Content = new WriteStreamContent(async (requestBodyStream) =>
                {
                    if (symmetricConfig != null)
                    {
                        FinalBlockStream requestBodyCryptoStream = null;
                        try
                        {
                            requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                            await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
#if NET5_0_OR_GREATER
                            await requestBodyCryptoStream.FlushFinalBlockAsync();
#else
                            requestBodyCryptoStream.FlushFinalBlock();
#endif
                        }
                        finally
                        {
                            if (requestBodyCryptoStream != null)
                            {
#if NETSTANDARD2_0
                                requestBodyCryptoStream.Dispose();
#else
                                await requestBodyCryptoStream.DisposeAsync();
#endif
                            }
                        }
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                        requestBodyStream.Flush();
                    }
                });

                request.Headers.Add(HttpCommon.ProviderTypeHeader, data.ProviderType);
                request.Content.Headers.ContentType = contentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };
                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                request.Headers.Host = serviceUrl.Authority;
                request.Headers.Add(HttpCommon.OriginHeader, serviceUrl.Host);

                if (authHeaders != null)
                {
                    foreach (var authHeader in authHeaders)
                    {
                        foreach (var authHeaderValue in authHeader.Value)
                            request.Headers.Add(authHeader.Key, authHeaderValue);
                    }
                }

                response = await client.SendAsync(request);
                responseBodyStream = await response.Content.ReadAsStreamAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                    throw responseException;
                }

#if NETSTANDARD2_0
                responseBodyStream.Dispose();
#else
                await responseBodyStream.DisposeAsync();
#endif
            }
            catch
            {
                if (responseBodyStream != null)
                {
#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                }
                if (response != null)
                    response.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}