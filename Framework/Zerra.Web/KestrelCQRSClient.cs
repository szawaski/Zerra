// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.IO;
using Zerra.Logging;

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
        private readonly INetworkSerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string? route;
        private readonly Uri routeUri;
        private readonly HttpClientHandler handler;
        private readonly HttpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsClient"/> class.
        /// </summary>
        /// <param name="serviceUrl">The URL of the remote Kestrel server (e.g., "http://localhost:9001").</param>
        /// <param name="serializer">The serializer for request/response serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor/decryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="authorizer">Optional authorizer for providing custom authentication headers.</param>
        /// <param name="route">Optional route path to append to the service URL (e.g., "/cqrs").</param>
        /// <param name="maxConcurrentQueries">Optional maximum concurrent queries for each query interface; defaults to ProcessorCount * 32.</param>
        /// <param name="maxConcurrentCommands">Optional maximum concurrent commands for each topic; defaults to ProcessorCount * 8.</param>
        /// <param name="maxConcurrentEvents">Optional maximum concurrent events for each topic; defaults to ProcessorCount * 16.</param>
        public KestrelCqrsClient(string serviceUrl, INetworkSerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, ICqrsAuthorizer? authorizer, string? route, int? maxConcurrentQueries = null, int? maxConcurrentCommands = null, int? maxConcurrentEvents = null) : base(serviceUrl, log, maxConcurrentQueries, maxConcurrentCommands, maxConcurrentEvents)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.authorizer = authorizer;

            this.route = route;
            if (route is not null)
                routeUri = new Uri($"{base.serviceUri.AbsoluteUri.TrimEnd('/')}/{route.TrimStart('/')}");
            else
                routeUri = base.serviceUri;

            this.handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer()
            };
            this.client = new HttpClient(this.handler);
        }

        /// <inheritdoc />
        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, Uri? overrideServiceUri, IEncryptor? overrideEncryptor)
        {
            var callUri = overrideServiceUri is null ? routeUri : (route is null ? overrideServiceUri : new Uri($"{overrideServiceUri.AbsoluteUri.TrimEnd('/')}/{route.TrimStart('/')}"));
            var providerName = interfaceType.Name;

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

            //a trailing CancellationToken isn't sent, the server passes its own in its place
            var serializeCount = argumentTypes.Count > 0 && argumentTypes[argumentTypes.Count - 1] == typeof(CancellationToken) ? argumentTypes.Count - 1 : argumentTypes.Count;
            data.ProviderArguments = new byte[argumentTypes.Count][];
            Stream? argumentStream = null;
            var hasArgumentStream = false;
            for (var i = 0; i < serializeCount; i++)
            {
                if (argumentTypes[i] == typeof(Stream))
                {
                    if (hasArgumentStream)
                        throw new ArgumentException($"{interfaceType.Name}.{methodName} can only have one Stream argument");
                    hasArgumentStream = true;
                    argumentStream = (Stream?)arguments[i];
                    continue;
                }
                data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);
            }

            var model = Request<TReturn>(throttle, isStream, callUri, overrideEncryptor ?? encryptor, providerName, data, argumentStream, true);
            return model;
        }

        /// <inheritdoc />
        protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, Uri? overrideServiceUri, IEncryptor? overrideEncryptor, CancellationToken cancellationToken) where TReturn : default
        {
            var callUri = overrideServiceUri is null ? routeUri : (route is null ? overrideServiceUri : new Uri($"{overrideServiceUri.AbsoluteUri.TrimEnd('/')}/{route.TrimStart('/')}"));
            var providerName = interfaceType.Name;

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

            //a trailing CancellationToken isn't sent, the server passes its own in its place
            var serializeCount = argumentTypes.Count > 0 && argumentTypes[argumentTypes.Count - 1] == typeof(CancellationToken) ? argumentTypes.Count - 1 : argumentTypes.Count;
            data.ProviderArguments = new byte[argumentTypes.Count][];
            Stream? argumentStream = null;
            var hasArgumentStream = false;
            for (var i = 0; i < serializeCount; i++)
            {
                if (argumentTypes[i] == typeof(Stream))
                {
                    if (hasArgumentStream)
                        throw new ArgumentException($"{interfaceType.Name}.{methodName} can only have one Stream argument");
                    hasArgumentStream = true;
                    argumentStream = (Stream?)arguments[i];
                    continue;
                }
                data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);
            }

            var model = RequestAsync<TReturn>(throttle, isStream, callUri, overrideEncryptor ?? encryptor, providerName, data, argumentStream, true, cancellationToken);
            return model;
        }

        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken)
        {
            var messageType = commandType.Name;
            var messageData = serializer.SerializeBytes(command, commandType);

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

            return RequestAsync<object>(throttle, false, routeUri, encryptor, commandType.Name, data, null, false, cancellationToken);
        }
        /// <inheritdoc />
        protected override Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default
        {
            var messageType = commandType.Name;
            var messageData = serializer.SerializeBytes(command, commandType);

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

            return RequestAsync<TResult>(throttle, isStream, routeUri, encryptor, commandType.Name, data, null, true, cancellationToken)!;
        }
        /// <inheritdoc />
        protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken)
        {
            var messageType = eventType.Name;
            var messageData = serializer.SerializeBytes(@event, eventType);

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

            return RequestAsync<object>(throttle, false, routeUri, encryptor, eventType.Name, data, null, false, cancellationToken);
        }

        private async Task<TReturn> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, IEncryptor? callEncryptor, string sourceName, CqrsRequestData data, Stream? argumentStream, bool getResponseData, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);

            HttpResponseMessage? response = null;
            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    Stream writeStream = postStream;
                    CryptoFlushStream? cryptoStream = null;
                    Stream? compressStream = null;
                    if (callEncryptor is not null)
                        writeStream = cryptoStream = callEncryptor.Encrypt(new LeaveOpenStream(postStream), true);
                    if (compressor is not null)
                        writeStream = compressStream = compressor.Compress(writeStream, true);
                    if (argumentStream is null)
                    {
                        await serializer.SerializeAsync(writeStream, data, cancellationToken);
                    }
                    else
                    {
                        using (var uploadDataStream = new TcpProtocolBodyStream(writeStream, null, true, true, default, false))
                        {
                            await serializer.SerializeAsync(uploadDataStream, data, cancellationToken);
                            await uploadDataStream.FlushAsync(cancellationToken);
                        }
#if NETSTANDARD2_0
                        await argumentStream.CopyToAsync(writeStream, 81920, cancellationToken);
#else
                        await argumentStream.CopyToAsync(writeStream, cancellationToken);
#endif
                    }
                    if (compressStream is not null)
                    {
#if NETSTANDARD2_0
                        compressStream.Dispose();
#else
                        await compressStream.DisposeAsync();
#endif
                    }
                    if (cryptoStream is not null)
                    {
#if NETSTANDARD2_0
                        cryptoStream.FlushFinalBlock();
#else
                        await cryptoStream.FlushFinalBlockAsync(cancellationToken);
#endif
#if NETSTANDARD2_0
                        cryptoStream.Dispose();
#else
                        await cryptoStream.DisposeAsync();
#endif
                    }
                });
                if (argumentStream is not null)
                    request.Headers.Add(HttpCommon.UploadStreamHeader, HttpCommon.UploadStreamValue);
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

                request.Headers.Add(HttpCommon.OriginHeader, url.Host); //the same as HttpCqrsClient, a server with allowed origins requires one

                //headers only so the body streams instead of buffering, the response stays undisposed for a stream result
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

#if NETSTANDARD2_0
                responseStream = await response.Content.ReadAsStreamAsync();
#else
                responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif

                if (callEncryptor is not null)
                    responseStream = callEncryptor.Decrypt(responseStream, false);
                if (compressor is not null)
                    responseStream = compressor.Decompress(responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorSource = data.ProviderMethod is null ? sourceName : $"{sourceName}.{data.ProviderMethod}";
                    //an error without a body, such as a 401 or a 400 sent before the request is read, has no details to read so the status is the error
                    Exception ex;
                    if (response.Content.Headers.ContentLength == 0)
                        ex = new CqrsNetworkException($"Remote service responded {(int)response.StatusCode} {response.ReasonPhrase} without details for {errorSource}");
                    else
                        ex = await ExceptionSerializer.DeserializeAsync(errorSource, serializer, responseStream, cancellationToken);
                    throw ex;
                }

                if (!getResponseData)
                {
#if NETSTANDARD2_0
                    responseStream.Dispose();
#else
                    await responseStream.DisposeAsync();
#endif
                    response.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream;
                }
                else
                {
                    var result = await serializer.DeserializeAsync<TReturn>(responseStream, cancellationToken);
#if NETSTANDARD2_0
                    responseStream.Dispose();
#else
                    await responseStream.DisposeAsync();
#endif
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
#if NETSTANDARD2_0
                        responseStream.Dispose();
#else
                        await responseStream.DisposeAsync();
#endif
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

        private TReturn Request<TReturn>(SemaphoreSlim throttle, bool isStream, Uri url, IEncryptor? callEncryptor, string sourceName, CqrsRequestData data, Stream? argumentStream, bool getResponseData)
        {
#if NETSTANDARD2_0
            //HttpClient has no synchronous send in netstandard2.0
            throw new PlatformNotSupportedException($"{nameof(KestrelCqrsClient)} synchronous calls are not supported on this platform, use the async methods.");
#else
            throttle.Wait();

            HttpResponseMessage? response = null;
            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new WriteStreamContent((postStream) =>
                {
                    Stream writeStream = postStream;
                    CryptoFlushStream? cryptoStream = null;
                    Stream? compressStream = null;
                    if (callEncryptor is not null)
                        writeStream = cryptoStream = callEncryptor.Encrypt(new LeaveOpenStream(postStream), true);
                    if (compressor is not null)
                        writeStream = compressStream = compressor.Compress(writeStream, true);
                    if (argumentStream is null)
                    {
                        serializer.Serialize(writeStream, data);
                    }
                    else
                    {
                        using (var uploadDataStream = new TcpProtocolBodyStream(writeStream, null, true, true, default, false))
                        {
                            serializer.Serialize(uploadDataStream, data);
                            uploadDataStream.Flush();
                        }
                        argumentStream.CopyTo(writeStream);
                    }
                    if (compressStream is not null)
                        compressStream.Dispose();
                    if (cryptoStream is not null)
                    {
                        cryptoStream.FlushFinalBlock();
                        cryptoStream.Dispose();
                    }
                });
                if (argumentStream is not null)
                    request.Headers.Add(HttpCommon.UploadStreamHeader, HttpCommon.UploadStreamValue);
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

                request.Headers.Add(HttpCommon.OriginHeader, url.Host); //the same as HttpCqrsClient, a server with allowed origins requires one

                //headers only so the body streams instead of buffering, the response stays undisposed for a stream result
                response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);

                responseStream = response.Content.ReadAsStream();

                if (callEncryptor is not null)
                    responseStream = callEncryptor.Decrypt(responseStream, false);
                if (compressor is not null)
                    responseStream = compressor.Decompress(responseStream, false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorSource = data.ProviderMethod is null ? sourceName : $"{sourceName}.{data.ProviderMethod}";
                    //an error without a body, such as a 401 or a 400 sent before the request is read, has no details to read so the status is the error
                    Exception ex;
                    if (response.Content.Headers.ContentLength == 0)
                        ex = new CqrsNetworkException($"Remote service responded {(int)response.StatusCode} {response.ReasonPhrase} without details for {errorSource}");
                    else
                        ex = ExceptionSerializer.Deserialize(errorSource, serializer, responseStream);
                    throw ex;
                }

                if (!getResponseData)
                {
                    responseStream.Dispose();
                    response.Dispose();
                    return default!;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream;
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
#endif
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
    }
}