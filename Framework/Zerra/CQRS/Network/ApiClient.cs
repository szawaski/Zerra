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
using System.Threading;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A client for making API requests to externaly exposed CQRS services or Gateways.
    /// </summary>
    public sealed class ApiClient : CqrsClientBase
    {
        private readonly ContentType requestContentType;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly Uri routeUri;
        private readonly HttpClientHandler handler;
        private readonly HttpClient client;

        /// <summary>
        /// Creates a new API client.
        /// </summary>
        /// <param name="endpoint">The url of the service receiving CQRS requests.</param>
        /// <param name="contentType">The content type of the communications.</param>
        /// <param name="authorizer">An authorizer for adding headers needed for the server to validate requests.</param>
        /// <param name="route">Adds n route argument to the base endpoint url if needed.</param>
        public ApiClient(string endpoint, ContentType contentType, ICqrsAuthorizer? authorizer, string? route = null) : base(endpoint)
        {
            this.requestContentType = contentType;
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

        /// <inheritdoc />
        protected override TReturn? CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string?[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

            var data = new ApiRequestData()
            {
                ProviderType = providerName,
                ProviderMethod = methodName,

                Source = source
            };
            data.AddProviderArguments(arguments);

            var model = Request<TReturn>(throttle, isStream, routeUri, providerName, requestContentType, data, true);
            return model;
        }
        /// <inheritdoc />
        protected override Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string?[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

            var data = new ApiRequestData()
            {
                ProviderType = providerName,
                ProviderMethod = methodName,

                Source = source
            };
            data.AddProviderArguments(arguments);

            var model = RequestAsync<TReturn>(throttle, isStream, routeUri, providerName, requestContentType, data, true);
            return model;
        }

        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source)
        {
            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(command, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, commendTypeName, requestContentType, data, false);
        }
        /// <inheritdoc />
        protected override Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source) where TResult : default
        {
            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(command, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<TResult>(throttle, isStream, routeUri, commendTypeName, requestContentType, data, true);
        }

        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source)
        {
            var commendTypeName = eventType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(@event, eventType);

            var data = new ApiRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, commendTypeName, requestContentType, data, false);
        }

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(ApiClient)).MethodDetailsBoxed.First(x => x.MethodInfo.Name == nameof(ApiClient.RequestAsync)).MethodInfo;
        private TReturn? Request<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, ContentType contentType, ApiRequestData data, bool getResponseData)
        {
            throttle.Wait();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent((postStream) =>
                {
                    ContentTypeSerializer.Serialize(contentType, postStream, data);
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
                    var authHeaders = authorizer.GetAuthorizationHeadersAsync().GetAwaiter().GetResult();
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
        private async Task<TReturn?> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, ContentType contentType, ApiRequestData data, bool getResponseData)
        {
            await throttle.WaitAsync();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    await ContentTypeSerializer.SerializeAsync(contentType, postStream, data);
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
                    var authHeaders = await authorizer.GetAuthorizationHeadersAsync();
                    foreach (var authHeader in authHeaders)
                        request.Headers.Add(authHeader.Key, authHeader.Value);
                }

                if (!String.IsNullOrWhiteSpace(providerType))
                    request.Headers.Add(HttpCommon.ProviderTypeHeader, providerType);

                using var response = await client.SendAsync(request);

                responseStream = await response.Content.ReadAsStreamAsync();

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

        /// <inheritdoc />
        public new void Dispose()
        {
            base.Dispose();
            client.Dispose();
            handler.Dispose();
        }
    }
}