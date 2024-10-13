// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using Zerra.Logging;
using System.Collections.Generic;
using System.Net.Http;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using System.Net.Http.Headers;
using Zerra.Encryption;
using Zerra.Serialization.Json;
using System.Security.Claims;
using System.Threading;
using System.Linq;

namespace Zerra.Web
{
    public sealed class KestrelCQRSClient : CqrsClientBase, IDisposable
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly HttpClient client;

        public KestrelCQRSClient(ContentType contentType, string serviceUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            this.client = new HttpClient();

            _ = Log.InfoAsync($"{nameof(KestrelCQRSClient)} started for {this.contentType} at {this.serviceUri}");
        }

        protected override TReturn? CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            throttle.Wait();

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, serviceUri);
                HttpResponseMessage? response = null;
                Stream? responseBodyStream = null;
                try
                {
                    request.Content = new WriteStreamContent((requestBodyStream) =>
                    {
                        if (symmetricConfig is not null)
                        {
                            CryptoFlushStream? requestBodyCryptoStream = null;
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
                    request.Headers.Host = serviceUri.Authority;
                    request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host);

                    if (authHeaders is not null)
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
                    responseBodyStream?.Dispose();
                    response?.Dispose();
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        protected override async Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            await throttle.WaitAsync();

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, serviceUri);
                HttpResponseMessage? response = null;
                Stream? responseBodyStream = null;
                try
                {
                    request.Content = new WriteStreamContent(async (requestBodyStream) =>
                    {
                        if (symmetricConfig is not null)
                        {
                            CryptoFlushStream? requestBodyCryptoStream = null;
                            try
                            {
                                requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                                await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
                                await requestBodyCryptoStream.FlushFinalBlockAsync();
                            }
                            finally
                            {
                                if (requestBodyCryptoStream is not null)
                                {
                                    await requestBodyCryptoStream.DisposeAsync();
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
                    request.Headers.Host = serviceUri.Authority;
                    request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host);

                    if (authHeaders is not null)
                    {
                        foreach (var authHeader in authHeaders)
                        {
                            foreach (var authHeaderValue in authHeader.Value)
                                request.Headers.Add(authHeader.Key, authHeaderValue);
                        }
                    }

                    response = await client.SendAsync(request);
                    responseBodyStream = await response.Content.ReadAsStreamAsync();

                    if (symmetricConfig is not null)
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
                        await responseBodyStream.DisposeAsync();
                        response.Dispose();
                        return model;
                    }
                }
                catch
                {
                    if (responseBodyStream is not null)
                    {
                        await responseBodyStream.DisposeAsync();
                    }
                    response?.Dispose();
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        protected override async Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source)
        {
            await throttle.WaitAsync();

            try
            {
                var messageTypeName = commandType.GetNiceName();

                var messageData = ContentTypeSerializer.Serialize(contentType, command);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = messageAwait,
                    MessageResult = false,

                    Claims = claims,
                    Source = source
                };

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, serviceUri);
                HttpResponseMessage? response = null;
                Stream? responseBodyStream = null;
                try
                {
                    request.Content = new WriteStreamContent(async (requestBodyStream) =>
                    {
                        if (symmetricConfig is not null)
                        {
                            CryptoFlushStream? requestBodyCryptoStream = null;
                            try
                            {
                                requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                                await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
                                await requestBodyCryptoStream.FlushFinalBlockAsync();
                            }
                            finally
                            {
                                if (requestBodyCryptoStream is not null)
                                {
                                    await requestBodyCryptoStream.DisposeAsync();
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
                    request.Headers.Host = serviceUri.Authority;
                    request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host);

                    if (authHeaders is not null)
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

                    await responseBodyStream.DisposeAsync();
                }
                catch
                {
                    if (responseBodyStream is not null)
                    {
                        await responseBodyStream.DisposeAsync();
                    }
                    response?.Dispose();
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        protected override async Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source) where TResult : default
        {
            await throttle.WaitAsync();

            try
            {
                var messageTypeName = commandType.GetNiceName();

                var messageData = ContentTypeSerializer.Serialize(contentType, command);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = true,
                    MessageResult = true,

                    Claims = claims,
                    Source = source
                };

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var request = new HttpRequestMessage(HttpMethod.Post, serviceUri);
                HttpResponseMessage? response = null;
                Stream? responseBodyStream = null;
                try
                {
                    request.Content = new WriteStreamContent(async (requestBodyStream) =>
                    {
                        if (symmetricConfig is not null)
                        {
                            CryptoFlushStream? requestBodyCryptoStream = null;
                            try
                            {
                                requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true, true);
                                await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
                                await requestBodyCryptoStream.FlushFinalBlockAsync();
                            }
                            finally
                            {
                                if (requestBodyCryptoStream is not null)
                                {
                                    await requestBodyCryptoStream.DisposeAsync();
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
                    request.Headers.Host = serviceUri.Authority;
                    request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host);

                    if (authHeaders is not null)
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

                    if (isStream)
                    {
                        return (TResult)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await ContentTypeSerializer.DeserializeAsync<TResult>(contentType, responseBodyStream);
                        await responseBodyStream.DisposeAsync();
                        response.Dispose();
                        return model;
                    }
                }
                catch
                {
                    if (responseBodyStream is not null)
                    {
                        await responseBodyStream.DisposeAsync();
                    }
                    response?.Dispose();
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            client.Dispose();
        }
    }
}