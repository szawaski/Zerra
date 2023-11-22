// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public sealed class ApiClient : CqrsClientBase
    {
        private readonly ContentType requestContentType;
        private CookieCollection cookies = null;

        public ApiClient(string endpoint, ContentType contentType) : base(endpoint)
        {
            this.requestContentType = contentType;
        }

        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string[arguments.Length];
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

        protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string[arguments.Length];
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

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(ApiClient)).MethodDetails.First(x => x.MethodInfo.Name == nameof(ApiClient.RequestAsync)).MethodInfo;
        private TReturn Request<TReturn>(SemaphoreSlim throttle, bool isStream, string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            throttle.Wait();

            HttpClient client = null;
            Stream responseStream = null;
            try
            {
                var cookieContainer = new CookieContainer();
                if (cookies != null)
                    cookieContainer.Add(cookies);

                using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
                client = new HttpClient(handler);

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

                this.cookies = GetCookiesFromContainer(cookieContainer);

                if (!getResponseData)
                {
                    responseStream.Dispose();
                    client.Dispose();
                    return default;
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
                if (responseStream != null)
                {
                    try
                    {
                        responseStream.Dispose();
                    }
                    catch { }
                    client.Dispose();
                }
                throw;
            }
            finally
            {
                throttle.Release();
            }
        }
        private async Task<TReturn> RequestAsync<TReturn>(SemaphoreSlim throttle, bool isStream, string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            await throttle.WaitAsync();

            HttpClient client = null;
            Stream responseStream = null;
            try
            {
                var cookieContainer = new CookieContainer();
                if (cookies != null)
                    cookieContainer.Add(cookies);

                using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
                client = new HttpClient(handler);

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

                this.cookies = GetCookiesFromContainer(cookieContainer);

                if (!getResponseData)
                {
                    responseStream.Dispose();
                    client.Dispose();
                    return default;
                }

                if (isStream)
                {
                    return (TReturn)(object)responseStream; //TODO better way to convert type???
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
                if (responseStream != null)
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
                    client.Dispose();
                }
                throw;
            }
            finally
            {
                throttle.Release();
            }
        }

        public CookieCollection GetCookieCredentials()
        {
            return this.cookies;
        }
        public void SetCookieCredentials(CookieCollection cookies)
        {
            this.cookies = cookies;
        }
        public void RemoveCookieCredentials()
        {
            this.cookies = null;
        }
        public Task RequestCookieCredentials(string address, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            return RequestAsync<object>(null, false, address, null, ContentType.Json, bytes, false);
        }

        private static readonly Func<object, object> cookieContainerGetter = TypeAnalyzer.GetTypeDetail(typeof(CookieContainer)).GetMember("m_domainTable").Getter;
        private static readonly Func<object, object> pathListGetter = TypeAnalyzer.GetTypeDetail(Discovery.GetTypeFromName("System.Net.PathList, System.Net.Primitives, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")).GetMember("m_list").Getter;
        private static CookieCollection GetCookiesFromContainer(CookieContainer cookieJar)
        {
            var cookieCollection = new CookieCollection();

            var table = (Hashtable)cookieContainerGetter(cookieJar);

            foreach (var pathList in table.Values)
            {
                var list = (SortedList)pathListGetter(pathList);

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
    }
}