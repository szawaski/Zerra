// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.IO;
using Zerra.Logging;
using Zerra.Serialization;

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

        /// <inheritdoc />
        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source)
        {
            var providerName = interfaceType.Name;

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

            data.ProviderArguments = new byte[argumentTypes.Count][];
            for (var i = 0; i < argumentTypes.Count; i++)
                data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);

            var model = Request<TReturn>(throttle, isStream, routeUri, providerName, data, true);
            return model;
        }

        /// <inheritdoc />
        protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken) where TReturn : default
        {
            var providerName = interfaceType.Name;

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

            data.ProviderArguments = new byte[argumentTypes.Count][];
            for (var i = 0; i < argumentTypes.Count; i++)
                data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);

            var model = RequestAsync<TReturn>(throttle, isStream, routeUri, providerName, data, true, cancellationToken);
            return model;
        }

        /// <inheritdoc />
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
        /// <inheritdoc />
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
                MessageResult = true,

                Claims = claims,
                Source = source
            };

            return RequestAsync<TResult>(throttle, isStream, routeUri, messageType, data, true, cancellationToken)!;
        }
        /// <inheritdoc />
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

            HttpResponseMessage? response = null;
            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    if (encryptor is not null)
                    {
                        var cryptoStream = encryptor.Encrypt(new LeaveOpenStream(postStream), true);
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

                if (authorizer is not null)
                {
                    var authHeaders = await authorizer.GetAuthorizationHeadersAsync();
                    foreach (var authHeader in authHeaders)
                        request.Headers.Add(authHeader.Key, authHeader.Value);
                }

                if (!String.IsNullOrWhiteSpace(providerType))
                    request.Headers.Add(HttpCommon.ProviderTypeHeader, providerType);

                request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host); //the same as HttpCqrsClient, a server with allowed origins requires one

                //headers only so the body streams instead of buffering, the response stays undisposed for a stream result
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                if (encryptor is not null)
                    responseStream = encryptor.Decrypt(responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ExceptionSerializer.DeserializeAsync(providerType ?? String.Empty, serializer, responseStream, cancellationToken);
                    throw responseException;
                }

                if (!getResponseData)
                {
                    await responseStream.DisposeAsync();
                    response.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = await serializer.DeserializeAsync<TReturn>(responseStream, cancellationToken);
                    await responseStream.DisposeAsync();
                    response.Dispose();
                    return result!;
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
                }
                response?.Dispose(); //the client is shared by every request so only the response is disposed
                throw;
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        private TReturn Request<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, string? providerType, CqrsRequestData data, bool getResponseData)
        {
            throttle.Wait();

            HttpResponseMessage? response = null;
            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent((postStream) =>
                {
                    if (encryptor is not null)
                    {
                        var cryptoStream = encryptor.Encrypt(new LeaveOpenStream(postStream), true);
                        serializer.Serialize(cryptoStream, data);
                        cryptoStream.FlushFinalBlock();
                        cryptoStream.Dispose();
                    }
                    else
                    {
                        serializer.Serialize(postStream, data);
                    }
                });
                request.Content.Headers.ContentType = serializer.ContentType switch
                {
                    ContentType.Bytes => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeBytes),
                    ContentType.Json => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson),
                    ContentType.JsonNameless => MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJsonNameless),
                    _ => throw new NotImplementedException(),
                };

                if (authorizer is not null)
                {
                    var authHeaders = authorizer.GetAuthorizationHeaders();
                    foreach (var authHeader in authHeaders)
                        request.Headers.Add(authHeader.Key, authHeader.Value);
                }

                if (!String.IsNullOrWhiteSpace(providerType))
                    request.Headers.Add(HttpCommon.ProviderTypeHeader, providerType);

                request.Headers.Add(HttpCommon.OriginHeader, serviceUri.Host); //the same as HttpCqrsClient, a server with allowed origins requires one

                //headers only so the body streams instead of buffering, the response stays undisposed for a stream result
                response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);

                responseStream = response.Content.ReadAsStream();

                if (encryptor is not null)
                    responseStream = encryptor.Decrypt(responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = ExceptionSerializer.Deserialize(providerType ?? String.Empty, serializer, responseStream);
                    throw responseException;
                }

                if (!getResponseData)
                {
                    responseStream.Dispose();
                    response.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream; //TODO better way to convert type???
                }
                else
                {
                    var result = serializer.Deserialize<TReturn>(responseStream);
                    responseStream.Dispose();
                    response.Dispose();
                    return result!;
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
                }
                response?.Dispose(); //the client is shared by every request so only the response is disposed
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
        public override void Dispose()
        {
            base.Dispose();
            client.Dispose();
            handler.Dispose();
        }

        //the request stream belongs to HttpClient, the encryption stream closes what it wraps when disposed
        private sealed class LeaveOpenStream(Stream stream) : StreamWrapper(stream, true) { }
    }
}