// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Text;

namespace Zerra.CQRS.Network
{
    public static class CookieParser
    {
        public static unsafe IDictionary<string, string> CookiesFromString(string cookieString)
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

        public static string CookiesToString(IDictionary<string, string> cookies)
        {
            var sb = new StringBuilder();
            foreach (var cookie in cookies)
            {
                if (sb.Length > 0)
                    _ = sb.Append("; ");
                _ = sb.Append(cookie.Key).Append('=').Append(cookie.Value);
            }
            return sb.ToString();
        }
    }
}