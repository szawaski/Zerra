// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETCOREAPP

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Implements CQRS authorization using HTTP cookies obtained from a login endpoint.
    /// </summary>
    public class ApiCqrsCookieAuthorizer : ICqrsAuthorizer
    {
        private const string cookieHeader = "Cookie";

        private readonly ISerializer serializer;
        private readonly Uri endpoint;
        private readonly string loginRequestBody;
        private readonly string contentType;
        private CookieCollection? cookies;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCqrsCookieAuthorizer"/> class.
        /// </summary>
        /// <param name="serializer">A JSON serializer for handling request and response serialization.</param>
        /// <param name="loginEndpoint">The URI endpoint for the login request. Can include or omit the URI scheme.</param>
        /// <param name="loginRequestBody">The request body to send to the login endpoint.</param>
        /// <param name="contentType">The content type for the login request.</param>
        /// <exception cref="ArgumentException">Thrown when the serializer is not a JSON serializer.</exception>
        public ApiCqrsCookieAuthorizer(ISerializer serializer, string loginEndpoint, string loginRequestBody, string contentType)
        {
            if (serializer.ContentType != ContentType.Json)
                throw new ArgumentException("Must be JSON serializer", nameof(serializer));

            this.serializer = serializer;
            if (!loginEndpoint.Contains("://"))
                this.endpoint = new Uri($"tcp://{loginEndpoint}"); //hacky way to make it parse without scheme.
            else
                this.endpoint = new Uri(loginEndpoint, UriKind.RelativeOrAbsolute);

            this.loginRequestBody = loginRequestBody;
            this.contentType = contentType;
        }

        private static async Task<CookieCollection> GetCookiesRequest(ISerializer serializer, Uri endpoint, string body, string contentType, CancellationToken cancellationToken)
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
                    await serializer.SerializeAsync(postStream, data, cancellationToken);
                });
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                request.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                request.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");

                using var response = await client.SendAsync(request, cancellationToken);

                responseStream = await response.Content.ReadAsStreamAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var responseException = await ExceptionSerializer.DeserializeAsync(serializer, responseStream, cancellationToken);
                    throw responseException;
                }

                var cookies = handler.CookieContainer.GetAllCookies();
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

        /// <summary>
        /// Parses a cookie header string into a dictionary of cookie names and values.
        /// </summary>
        /// <param name="cookieString">The cookie header string to parse.</param>
        /// <returns>A dictionary containing the parsed cookie names and values.</returns>
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

        /// <inheritdoc/>
        public Task Login(CancellationToken cancellationToken = default) => GetCookiesRequest(serializer, endpoint, loginRequestBody, contentType, cancellationToken);

        /// <summary>
        /// Gets the cookies obtained from the login request.
        /// </summary>
        public CookieCollection? Cookies => cookies;

        /// <inheritdoc/>
        public void Authorize(Dictionary<string, List<string?>> headers)
        {
            Dictionary<string, string>? cookies = null;

            if (headers.TryGetValue(cookieHeader, out var cookieHeaderValue))
                cookies = CookiesFromString(cookieHeader);

            AuthorizeCookies(cookies);
        }

        /// <inheritdoc/>
        public async ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default)
        {
            cookies ??= await GetCookiesRequest(serializer, endpoint, loginRequestBody, contentType, cancellationToken);

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

        /// <summary>
        /// Authorizes cookies. This method must be overridden by derived classes to implement cookie authorization logic.
        /// </summary>
        /// <param name="cookies">The cookies to authorize, or null if no cookies are available.</param>
        /// <exception cref="NotImplementedException">Thrown when this method is not overridden by a derived class.</exception>
        public virtual void AuthorizeCookies(Dictionary<string, string>? cookies) => throw new NotImplementedException($"{nameof(ApiCqrsCookieAuthorizer)}.{nameof(AuthorizeCookies)} needs overridden to use.");
    }
}

#endif