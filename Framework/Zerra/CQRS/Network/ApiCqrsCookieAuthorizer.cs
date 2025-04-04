// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    public class ApiCqrsCookieAuthorizer : ICqrsAuthorizer
    {
        private const string cookieHeader = "Cookie";

        private readonly Uri endpoint;
        private readonly string loginRequestBody;
        private readonly string contentType;
        private CookieCollection? cookies;

        public ApiCqrsCookieAuthorizer(string loginEndpoint, string loginRequestBody, string contentType)
        {
            if (!loginEndpoint.Contains("://"))
                this.endpoint = new Uri($"tcp://{loginEndpoint}"); //hacky way to make it parse without scheme.
            else
                this.endpoint = new Uri(loginEndpoint, UriKind.RelativeOrAbsolute);

            this.loginRequestBody = loginRequestBody;
            this.contentType = contentType;
        }

        private static async Task<CookieCollection> GetCookiesRequest(Uri endpoint, string body, string contentType, CancellationToken cancellationToken)
        {
            using var handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer()
            };
            using var client = new HttpClient(handler);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            Stream? responseStream = null;
            try
            {
                request.Content = new WriteStreamContent(async (postStream) =>
                {
                    var data = System.Text.Encoding.UTF8.GetBytes(body);
                    await ContentTypeSerializer.SerializeAsync(ContentType.Json, postStream, data, cancellationToken);
                });
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

                using var response = await client.SendAsync(request, cancellationToken);

                responseStream = await response.Content.ReadAsStreamAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(ContentType.Json, responseStream, cancellationToken);
                    throw responseException;
                }

                var cookies = GetCookiesFromContainer(handler.CookieContainer);
                return cookies;
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

        public static unsafe Dictionary<string, string> CookiesFromString(string cookieString)
        {
            var cookies = new Dictionary<string, string>();
            var chars = cookieString.AsSpan();

            var startIndex = 0;
            var indexLength = 0;
            string? key = null;
            fixed (char* pChars = chars)
            {
                for (var index = 0; index < chars.Length; index++)
                {
                    var c = pChars[index];
                    switch (c)
                    {
                        case '=':
                            if (key == null)
                            {
                                key = chars.Slice(startIndex, indexLength).ToString();
                                startIndex = index + 1;
                                indexLength = 0;
                            }
                            else
                            {
                                indexLength++;
                            }
                            break;
                        case ';':
                            if (key != null)
                            {
                                var value = chars.Slice(startIndex, indexLength).ToString();
#if NETSTANDARD2_0
                                if (!cookies.ContainsKey(key))
                                    cookies.Add(key, value);
#else
                                _ = cookies.TryAdd(key, value);
#endif
                                key = null;
                            }
                            startIndex = index + 1;
                            indexLength = 0;
                            break;
                        case ' ':
                            if (indexLength != 0)
                            {
                                indexLength++;
                            }
                            break;
                        default:
                            indexLength++;
                            break;

                    }
                }
            }

            if (key != null)
            {
                var value = chars.Slice(startIndex, indexLength).ToString();
#if NETSTANDARD2_0
                if (!cookies.ContainsKey(key))
                    cookies.Add(key, value);
#else
                _ = cookies.TryAdd(key, value);
#endif
            }

            return cookies;
        }

        public Task Login(CancellationToken cancellationToken = default) => GetCookiesRequest(endpoint, loginRequestBody, contentType, cancellationToken);

        public CookieCollection? Cookies => cookies;

        public void Authorize(Dictionary<string, List<string?>> headers)
        {
            Dictionary<string, string>? cookies = null;

            if (headers.TryGetValue(cookieHeader, out var cookieHeaderValue))
                cookies = CookiesFromString(cookieHeader);

            AuthorizeCookies(cookies);
        }

        public async ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default)
        {
            cookies ??= await GetCookiesRequest(endpoint, loginRequestBody, contentType, cancellationToken);

            var sb = new StringBuilder();
            foreach (Cookie cookie in cookies)
            {
                if (sb.Length > 0)
                    _ = sb.Append("; ");
                _ = sb.Append(cookie.Name).Append('=').Append(cookie.Value);
            }
            var cookieString = sb.ToString();

            var authHeaders = new Dictionary<string, List<string?>>
            {
                { cookieHeader, [cookieString] }
            };

            return authHeaders;
        }

        public virtual void AuthorizeCookies(Dictionary<string, string>? cookies) => throw new NotImplementedException($"{nameof(ApiCqrsCookieAuthorizer)}.{nameof(AuthorizeCookies)} needs overridden to use.");
    }
}