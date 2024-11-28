// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Reflection;
using Zerra.Serialization.Json;

namespace Zerra.Web
{
    public sealed class KestrelCqrsClient : CqrsClientBase
    {
        private readonly ContentType requestContentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly Uri routeUri;
        private readonly HttpClientHandler handler;
        private readonly HttpClient client;

        public KestrelCqrsClient(string endpoint, ContentType contentType, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer, string? route) : base(endpoint)
        {
            this.requestContentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;

            if (route is not null)
                routeUri = new Uri($"{base.serviceUri.AbsoluteUri}{route}");
            else
                routeUri = base.serviceUri;

            this.handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer()
            };
            this.client = new HttpClient(this.handler);
        }

        protected override TReturn? CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string?[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

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

            var model = Request<TReturn>(throttle, isStream, routeUri, providerName, requestContentType, data, true);
            return model;
        }
        protected override Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string?[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

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

            var model = RequestAsync<TReturn>(throttle, isStream, routeUri, providerName, requestContentType, data, true);
            return model;
        }

        protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source)
        {
            var messageType = commandType.GetNiceFullName();
            var messageData = ContentTypeSerializer.Serialize(requestContentType, command);

            string[][]? claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CqrsRequestData()
            {
                MessageType = messageType,
                MessageData = messageData,
                MessageAwait = messageAwait,
                MessageResult = false,

                Claims = claims,
                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, messageType, requestContentType, data, false);
        }
        protected override Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source) where TResult : default
        {
            var messageType = commandType.GetNiceFullName();
            var messageData = ContentTypeSerializer.Serialize(requestContentType, command);

            string[][]? claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CqrsRequestData()
            {
                MessageType = messageType,
                MessageData = messageData,
                MessageAwait = true,
                MessageResult = false,

                Claims = claims,
                Source = source
            };

            return RequestAsync<TResult>(throttle, isStream, routeUri, messageType, requestContentType, data, true);
        }

        protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source)
        {
            var messageType = eventType.GetNiceFullName();
            var messageData = ContentTypeSerializer.Serialize(requestContentType, @event);

            string[][]? claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var data = new CqrsRequestData()
            {
                MessageType = messageType,
                MessageData = messageData,
                MessageAwait = false,
                MessageResult = false,

                Claims = claims,
                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, messageType, requestContentType, data, false);
        }

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(KestrelCqrsClient)).MethodDetailsBoxed.First(x => x.MethodInfo.Name == nameof(KestrelCqrsClient.RequestAsync)).MethodInfo;
        private TReturn? Request<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, ContentType contentType, CqrsRequestData data, bool getResponseData)
        {
            throttle.Wait();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent((postStream) =>
                {
                    if (symmetricConfig is not null)
                    {
                        var cryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, postStream, true);
                        ContentTypeSerializer.Serialize(contentType, cryptoStream, data);
                        cryptoStream.FlushFinalBlock();
                        cryptoStream.Dispose();
                    }
                    else
                    {
                        ContentTypeSerializer.Serialize(contentType, postStream, data);
                    }
                });
                request.Content.Headers.ContentType = contentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };

                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

                if (authorizer is not null)
                {
                    var authHeaders = authorizer.BuildAuthHeaders();
                    foreach (var authHeader in authHeaders)
                        request.Headers.Add(authHeader.Key, authHeader.Value);
                }

                if (!String.IsNullOrWhiteSpace(providerType))
                    request.Headers.Add(HttpCommon.ProviderTypeHeader, providerType);


#if NET5_0_OR_GREATER
                using var response = client.Send(request);
                responseStream = response.Content.ReadAsStream();
#else
                using var response = client.SendAsync(request).GetAwaiter().GetResult();
                responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
#endif

                if (symmetricConfig is not null)
                    responseStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = ContentTypeSerializer.DeserializeException(contentType, responseStream);
                    throw responseException;
                }

                if (!getResponseData)
                {
                    responseStream.Dispose();
                    client.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = ContentTypeSerializer.Deserialize<TReturn>(contentType, responseStream);
                    responseStream.Dispose();
                    client.Dispose();
                    return result;
                }
            }
            catch
            {
                if (responseStream is not null)
                {
                    try
                    {
                        responseStream.Dispose();
                    }
                    catch { }
                    client?.Dispose();
                }
                throw;
            }
            finally
            {
                throttle.Release();
            }
        }
        private async Task<TReturn?> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, ContentType contentType, CqrsRequestData data, bool getResponseData)
        {
            await throttle.WaitAsync();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    if (symmetricConfig is not null)
                    {
                        var cryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, postStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, cryptoStream, data);
#if NETCOREAPP
                        await cryptoStream.FlushFinalBlockAsync();
#else
                        cryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                        cryptoStream.Dispose();
#else
                        await cryptoStream.DisposeAsync();
#endif
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, postStream, data);
                    }
                });
                request.Content.Headers.ContentType = contentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };

                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

                if (authorizer is not null)
                {
                    var authHeaders = authorizer.BuildAuthHeaders();
                    foreach (var authHeader in authHeaders)
                        request.Headers.Add(authHeader.Key, authHeader.Value);
                }

                if (!String.IsNullOrWhiteSpace(providerType))
                    request.Headers.Add(HttpCommon.ProviderTypeHeader, providerType);

                using var response = await client.SendAsync(request);

                responseStream = await response.Content.ReadAsStreamAsync();

                if (symmetricConfig is not null)
                    responseStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseStream);
                    throw responseException;
                }

                if (!getResponseData)
                {
#if NETSTANDARD2_0
                    responseStream.Dispose();
#else
                    await responseStream.DisposeAsync();
#endif
                    client.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn?)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseStream);
#if NETSTANDARD2_0
                    responseStream.Dispose();
#else
                    await responseStream.DisposeAsync();
#endif
                    client.Dispose();
                    return result;
                }
            }
            catch
            {
                if (responseStream is not null)
                {
                    try
                    {
#if NETSTANDARD2_0
                        responseStream.Dispose();
#else
                        await responseStream.DisposeAsync();
#endif
                    }
                    catch { }
                    client?.Dispose();
                }
                throw;
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
            handler.Dispose();
        }
    }
}