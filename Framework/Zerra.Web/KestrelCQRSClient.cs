// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Serialization.Json;

namespace Zerra.Web
{
    /// <summary>
    /// CQRS client for communicating with Kestrel-based CQRS servers over HTTP.
    /// </summary>
    /// <remarks>
    /// Implements client-side dispatch of commands, queries, and events to a remote Kestrel server.
    /// Supports optional message encryption, custom authorization, and multiple serialization formats.
    /// Manages HTTP connections and handles request/response serialization automatically.
    /// </remarks>
    public sealed class KestrelCqrsClient : CqrsClientBase
    {
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly Uri routeUri;
        private readonly HttpClientHandler handler;
        private readonly HttpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsClient"/> class.
        /// </summary>
        /// <param name="endpoint">The remote Kestrel server endpoint (e.g., "http://localhost:9001").</param>
        /// <param name="serializer">The serializer for request/response serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor/decryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="authorizer">Optional authorizer for providing custom authentication headers.</param>
        /// <param name="route">Optional route path to append to the endpoint (e.g., "/cqrs").</param>
        public KestrelCqrsClient(string endpoint, ISerializer serializer, IEncryptor? encryptor, ILogger? log, ICqrsAuthorizer? authorizer, string? route) : base(endpoint, log)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
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

        protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken) where TReturn : default
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
                ProviderType = interfaceType.AssemblyQualifiedName,
                ProviderMethod = methodName,

                Claims = claims,
                Source = source
            };
            data.ProviderArguments = arguments.Select(x => serializer.SerializeString(x)).ToArray();

            var model = RequestAsync<TReturn>(throttle, isStream, routeUri, providerName, data, true, cancellationToken);
            return model;
        }

        protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken)
        {
            var messageType = commandType.AssemblyQualifiedName;
            var messageData = serializer.SerializeBytes(command);

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

            return RequestAsync<object>(throttle, false, routeUri, messageType, data, false, cancellationToken);
        }
        protected override Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default
        {
            var messageType = commandType.AssemblyQualifiedName;
            var messageData = serializer.SerializeBytes(command);

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

            return RequestAsync<TResult>(throttle, isStream, routeUri, messageType, data, true, cancellationToken)!;
        }

        protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken)
        {
            var messageType = eventType.AssemblyQualifiedName;
            var messageData = serializer.SerializeBytes(@event);

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

            return RequestAsync<object>(throttle, false, routeUri, messageType, data, false, cancellationToken);
        }

        private async Task<TReturn> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, CqrsRequestData data, bool getResponseData, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    if (encryptor is not null)
                    {
                        var cryptoStream = encryptor.Encrypt(postStream, true);
                        await serializer.SerializeAsync(cryptoStream, data, cancellationToken);
                        await cryptoStream.FlushFinalBlockAsync(cancellationToken);
                        await cryptoStream.DisposeAsync();
                    }
                    else
                    {
                        await serializer.SerializeAsync(postStream, data, cancellationToken);
                    }
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

                using var response = await client.SendAsync(request, cancellationToken);

                responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                if (encryptor is not null)
                    responseStream = encryptor.Decrypt(responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ExceptionSerializer.DeserializeAsync(serializer, responseStream, cancellationToken);
                    throw responseException;
                }

                if (!getResponseData)
                {
                    await responseStream.DisposeAsync();
                    client.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn?)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = await serializer.DeserializeAsync<TReturn>(responseStream, cancellationToken);
                    await responseStream.DisposeAsync();
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
                        await responseStream.DisposeAsync();
                    }
                    catch { }
                    client?.Dispose();
                }
                throw;
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KestrelCqrsClient"/>.
        /// </summary>
        /// <remarks>
        /// Disposes the HTTP client and handler. After disposal, the client cannot be used.
        /// </remarks>
        public new void Dispose()
        {
            base.Dispose();
            client.Dispose();
            handler.Dispose();
        }
    }
}