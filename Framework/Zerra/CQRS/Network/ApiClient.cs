// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public sealed class ApiClient : IQueryClient, ICommandProducer
    {
        private readonly string endpointAddress;
        private readonly ContentType requestContentType;
        private readonly HashSet<Type> commandTypes;

        public string ConnectionString => endpointAddress;

        private CookieCollection cookies = null;

        public ApiClient(string endpointAddress, ContentType contentType)
        {
            this.endpointAddress = endpointAddress;
            this.requestContentType = contentType;
            this.commandTypes = new();
        }

        void ICommandProducer.RegisterCommandType(Type type)
        {
            lock (commandTypes)
            {
                if (commandTypes.Contains(type))
                    return;
                commandTypes.Add(type);
            }
        }
        IEnumerable<Type> ICommandProducer.GetCommandTypes()
        {
            return commandTypes;
        }

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(ApiClient)).MethodDetails.First(x => x.MethodInfo.Name == nameof(ApiClient.RequestAsync)).MethodInfo;
        TReturn IQueryClient.Call<TReturn>(Type interfaceType, string methodName, object[] arguments, string source)
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

            var returnType = typeof(TReturn);

            var data = new ApiRequestData()
            {
                ProviderType = providerName,
                ProviderMethod = methodName,

                Source = source
            };
            data.AddProviderArguments(arguments);

            var returnTypeDetails = TypeAnalyzer.GetTypeDetail(returnType);
            if (returnTypeDetails.IsTask)
            {
                var callRequestAsyncMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(requestAsyncMethod, returnTypeDetails.InnerTypes.ToArray());
                return (TReturn)callRequestAsyncMethodGeneric.Caller(this, new object[] { endpointAddress, providerName, requestContentType, data, true });
            }
            else
            {
                var model = Request<TReturn>(endpointAddress, providerName, requestContentType, data, true);
                return model;
            }
        }

        Task ICommandProducer.DispatchAsync(ICommand message, string source)
        {
            var commandType = message.GetType();
            if (!commandTypes.Contains(commandType))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(ApiClient)}");

            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(message, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = false,

                Source = source
            };

            return RequestAsync<object>(endpointAddress, commendTypeName, requestContentType, data, false);
        }

        Task ICommandProducer.DispatchAsyncAwait(ICommand message, string source)
        {
            var commandType = message.GetType();
            if (!commandTypes.Contains(commandType))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(ApiClient)}");

            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(message, commandType);

            var data = new ApiRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = true,

                Source = source
            };

            return RequestAsync<object>(endpointAddress, commendTypeName, requestContentType, data, false);
        }

        private TReturn Request<TReturn>(string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            var cookieContainer = new CookieContainer();
            if (cookies != null)
                cookieContainer.Add(cookies);

            using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

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
            using var responseStream = response.Content.ReadAsStream();
#else
            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            using var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
#endif

            if (!response.IsSuccessStatusCode)
            {
                var responseException = ContentTypeSerializer.DeserializeException(contentType, responseStream);
                throw responseException;
            }

            this.cookies = GetCookiesFromContainer(cookieContainer);

            var result = ContentTypeSerializer.Deserialize<TReturn>(contentType, responseStream);

            return result;
        }
        private async Task<TReturn> RequestAsync<TReturn>(string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            var cookieContainer = new CookieContainer();
            if (cookies != null)
                cookieContainer.Add(cookies);

            using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

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
#if NETSTANDARD2_0
            using var responseStream = await response.Content.ReadAsStreamAsync();
#else
            await using var responseStream = await response.Content.ReadAsStreamAsync();
#endif

            if (!response.IsSuccessStatusCode)
            {
                var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseStream);
                throw responseException;
            }

            this.cookies = GetCookiesFromContainer(cookieContainer);

            var result = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseStream);

            return result;
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
            return RequestAsync<object>(address, null, ContentType.Json, bytes, false);
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