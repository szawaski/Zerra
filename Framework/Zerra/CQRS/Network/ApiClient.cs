// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public class ApiClient : IQueryClient, ICommandProducer
    {
        private readonly string endpointAddress;
        private readonly ContentType requestContentType;

        public string ConnectionString => endpointAddress;

        public ApiClient(string endpointAddress, ContentType contentType)
        {
            this.endpointAddress = endpointAddress;
            this.requestContentType = contentType;
        }

        private CookieCollection cookies = null;

        private static readonly MethodInfo requestAsyncMethod = TypeAnalyzer.GetTypeDetail(typeof(ApiClient)).MethodDetails.First(x => x.MethodInfo.Name == nameof(ApiClient.RequestAsync)).MethodInfo;
        TReturn IQueryClient.Call<TReturn>(Type interfaceType, string methodName, object[] arguments)
        {
            var providerName = interfaceType.Name;
            var stringArguments = new string[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                stringArguments[i] = JsonSerializer.Serialize(arguments);

            var returnType = typeof(TReturn);

            var data = new CQRSRequestData()
            {
                ProviderType = providerName,
                ProviderMethod = methodName,
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

        Task ICommandProducer.DispatchAsync(ICommand message)
        {
            var commandType = message.GetType();
            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(message, commandType);

            var data = new CQRSRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = false
            };

            return RequestAsync<object>(endpointAddress, commendTypeName, requestContentType, data, false);
        }

        Task ICommandProducer.DispatchAsyncAwait(ICommand message)
        {
            var commandType = message.GetType();
            var commendTypeName = commandType.GetNiceFullName();
            var commandData = JsonSerializer.Serialize(message, commandType);

            var data = new CQRSRequestData()
            {
                MessageType = commendTypeName,
                MessageData = commandData,
                MessageAwait = true
            };

            return RequestAsync<object>(endpointAddress, commendTypeName, requestContentType, data, false);
        }

        private TReturn Request<TReturn>(string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            var request = WebRequest.CreateHttp(address);

            request.Method = "POST";
            switch (contentType)
            {
                case ContentType.Bytes:
                    request.ContentType = "application/octet-stream";
                    break;
                case ContentType.Json:
                    request.ContentType = "application/json; charset=utf-8";
                    break;
                case ContentType.JsonNameless:
                    request.ContentType = "application/jsonnameless; charset=utf-8";
                    break;
            }

            request.Accept = request.ContentType;
            request.Timeout = Timeout.Infinite;

            if (!String.IsNullOrWhiteSpace(providerType))
                request.Headers.Add("Provider-Type", providerType);

            var cookieContainer = new CookieContainer();
            if (cookies != null)
                cookieContainer.Add(cookies);
            request.CookieContainer = cookieContainer;

            request.SendChunked = true;
            using (var postStream = request.GetRequestStream())
            {
                ContentTypeSerializer.Serialize(contentType, postStream, data);
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                if (getResponseData)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var result = ContentTypeSerializer.Deserialize<TReturn>(contentType, stream);
                        return result;
                    }
                }

                response.Close();
                response.Dispose();
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    Exception innerException;
                    using (var exStream = ex.Response.GetResponseStream())
                    {
                        innerException = ContentTypeSerializer.DeserializeException(contentType, exStream);
                    }
                    ex.Response.Close();
                    ex.Response.Dispose();

                    throw innerException;
                }
                throw;
            }

            this.cookies = GetCookiesFromContainer(cookieContainer);

            return default;
        }
        private async Task<TReturn> RequestAsync<TReturn>(string address, string providerType, ContentType contentType, object data, bool getResponseData)
        {
            var request = WebRequest.CreateHttp(address);

            request.Method = "POST";
            switch (contentType)
            {
                case ContentType.Bytes:
                    request.ContentType = "application/octet-stream";
                    break;
                case ContentType.Json:
                    request.ContentType = "application/json; charset=utf-8";
                    break;
                case ContentType.JsonNameless:
                    request.ContentType = "application/jsonnameless; charset=utf-8";
                    break;
            }

            request.Accept = request.ContentType;
            request.Timeout = Timeout.Infinite;

            if (!String.IsNullOrWhiteSpace(providerType))
                request.Headers.Add("Provider-Type", providerType);

            var cookieContainer = new CookieContainer();
            if (cookies != null)
                cookieContainer.Add(cookies);
            request.CookieContainer = cookieContainer;

            request.SendChunked = true;
            using (var postStream = request.GetRequestStream())
            {
                await ContentTypeSerializer.SerializeAsync(contentType, postStream, data);
            }

            try
            {
                var response = (HttpWebResponse)await request.GetResponseAsync();

                if (getResponseData)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var result = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, stream);
                        return result;
                    }
                }

                response.Close();
                response.Dispose();
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    Exception innerException;
                    using (var exStream = ex.Response.GetResponseStream())
                    {
                        innerException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, exStream);
                    }
                    ex.Response.Close();
                    ex.Response.Dispose();

                    throw innerException;
                }
                throw;
            }

            this.cookies = GetCookiesFromContainer(cookieContainer);

            return default;
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
        public static CookieCollection GetCookiesFromContainer(CookieContainer cookieJar)
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