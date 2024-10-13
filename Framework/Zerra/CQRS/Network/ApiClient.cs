// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
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
    public sealed class ApiClient : CqrsClientBase
    {
        private readonly ContentType requestContentType;
        private readonly HttpClientHandler handler;
        private readonly HttpClient client;

        public ApiClient(string endpoint, ContentType contentType) : base(endpoint)
        {
            this.requestContentType = contentType;

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

            var data = new ApiRequestData()
            {
                ProviderType = providerName,
                ProviderMethod = methodName,

                Source = source
            };
            data.AddProviderArguments(arguments);

            var model = Request<TReturn>(throttle, isStream, serviceUri.OriginalString, providerName, requestContentType, data, true);
            return model;
        }

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

            var model = RequestAsync<TReturn>(throttle, isStream, serviceUri.OriginalString, providerName, requestContentType, data, true);
            return model;
        }

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

            return RequestAsync<object>(throttle, false, serviceUri.OriginalString, commendTypeName, requestContentType, data, false);
        }

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

            return RequestAsync<TResult>(throttle, isStream, serviceUri.OriginalString, commendTypeName, requestContentType, data, true);
        }

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(ApiClient)).MethodDetailsBoxed.First(x => x.MethodInfo.Name == nameof(ApiClient.RequestAsync)).MethodInfo;
        private TReturn? Request<TReturn>(SemaphoreSlim throttle, bool isStream, string address, string? providerType, ContentType contentType, object data, bool getResponseData)
        {
            throttle.Wait();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, address);

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

                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

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
        private async Task<TReturn?> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, string address, string? providerType, ContentType contentType, object data, bool getResponseData)
        {
            await throttle.WaitAsync();

            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, address);

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

                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

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
                    responseStream.Dispose();
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

        public CookieCollection? GetCookieCredentials()
        {
            return GetCookiesFromContainer(this.handler.CookieContainer);
        }
        public void SetCookieCredentials(CookieCollection cookies)
        {
            var newCookieContainer = new CookieContainer();
            newCookieContainer.Add(cookies);
            this.handler.CookieContainer = newCookieContainer;
        }
        public void RemoveCookieCredentials()
        {
            this.handler.CookieContainer = new CookieContainer();
        }
        public async Task RequestCookieCredentials(string address, string json)
        {
            Stream? responseStream = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, address);

                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    var data = System.Text.Encoding.UTF8.GetBytes(json);
                    await ContentTypeSerializer.SerializeAsync(ContentType.Json, postStream, data);
                });
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson);

                request.Headers.TransferEncodingChunked = true;
                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

                using var response = await client.SendAsync(request);

                responseStream = await response.Content.ReadAsStreamAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(ContentType.Json, responseStream);
                    throw responseException;
                }

                responseStream.Dispose();
                client.Dispose();

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
        }

        private static readonly Func<object, object?> cookieContainerGetter = TypeAnalyzer.GetTypeDetail(typeof(CookieContainer)).GetMember("m_domainTable").GetterBoxed;
        private static readonly Func<object, object?> pathListGetter = TypeAnalyzer.GetTypeDetail(Discovery.GetTypeFromName("System.Net.PathList, System.Net.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")).GetMember("m_list").GetterBoxed;
        private static CookieCollection GetCookiesFromContainer(CookieContainer cookieJar)
        {
            var cookieCollection = new CookieCollection();

            var table = (Hashtable)cookieContainerGetter(cookieJar)!;

            foreach (var pathList in table.Values)
            {
                var list = (SortedList)pathListGetter(pathList)!;

                foreach (CookieCollection collection in list.Values)
                {
                    foreach (var cookie in collection.Cast<Cookie>())
                    {
                        cookieCollection.Add(cookie);
                    }
                }
            }

            return cookieCollection;
        }

        public new void Dispose()
        {
            base.Dispose();
            client.Dispose();
            handler.Dispose();
        }
    }
}