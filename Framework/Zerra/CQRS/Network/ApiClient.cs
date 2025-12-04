// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Http.Headers;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A client for making API requests to externaly exposed CQRS services or Gateways.
    /// </summary>
    public sealed class ApiClient : CqrsClientBase
    {
        private readonly ISerializer serializer;
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
        public ApiClient(string endpoint, ISerializer serializer, ILog? log, ICqrsAuthorizer? authorizer, string? route = null) : base(endpoint, log)
        {
            this.serializer = serializer;
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
        protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken) where TReturn : default
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
            data.ProviderArguments = arguments.Select(x => serializer.SerializeString(x)).ToArray();

            var model = RequestAsync<TReturn>(throttle, isStream, routeUri, providerName, data, true, cancellationToken);
            return model;
        }

        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken)
        {
            var commandTypeName = commandType.AssemblyQualifiedName;
            var commandData = JsonSerializer.Serialize(command, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commandTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, commandTypeName, data, false, cancellationToken);
        }
        /// <inheritdoc />
        protected override Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default
        {
            var commandTypeName = commandType.AssemblyQualifiedName;
            var commandData = JsonSerializer.Serialize(command, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commandTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<TResult>(throttle, isStream, routeUri, commandTypeName, data, true, cancellationToken)!;
        }

        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken)
        {
            var commandTypeName = eventType.AssemblyQualifiedName;
            var commandData = JsonSerializer.Serialize(@event, eventType);

            var data = new ApiRequestData()
            {
                MessageType = commandTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<object>(throttle, false, routeUri, commandTypeName, data, false, cancellationToken);
        }

        private async Task<TReturn> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, ApiRequestData data, bool getResponseData, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    await serializer.SerializeAsync(postStream, data, cancellationToken);
                });
                request.Content.Headers.ContentType = serializer.ContentType switch
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
                    var responseException = await ExceptionSerializer.DeserializeAsync(serializer, responseStream, cancellationToken);
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
                    return (TReturn)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = await serializer.DeserializeAsync<TReturn>(responseStream, cancellationToken);
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