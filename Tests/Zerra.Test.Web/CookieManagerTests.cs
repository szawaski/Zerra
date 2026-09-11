// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class CookieManagerTests
    {
        private const int maxCookieSizeBytes = 4096;

        [Fact]
        public void Add_SmallValue_RoundTripsInOneCookie()
        {
            var value = "value";

            var setCookies = Add(value);

            Assert.Equal(["test"], setCookies.Keys);
            Assert.Equal(value, Get(setCookies));
        }

        [Theory]
        [InlineData('a', 10_000)] //sent as is
        [InlineData(';', 3_000)] //escaped to three bytes each
        [InlineData('é', 3_000)] //two UTF-8 bytes escaped to six each
        public void Add_LargeValue_SplitsWithinCookieSizeAndRoundTrips(char c, int length)
        {
            var value = new string(c, length);

            var setCookies = Add(value);

            Assert.True(setCookies.Count > 1);
            foreach (var setCookie in setCookies.Values)
                Assert.True(setCookie.Length <= maxCookieSizeBytes, $"Set-Cookie was {setCookie.Length} bytes, browsers drop cookies over {maxCookieSizeBytes}");
            Assert.Equal(value, Get(setCookies));
        }

        [Fact]
        public void Add_SurrogatePairs_AreNotSplit()
        {
            var value = String.Concat(Enumerable.Repeat("😀", 2_000));

            var setCookies = Add(value);

            Assert.True(setCookies.Count > 1);
            Assert.Equal(value, Get(setCookies));
        }

        [Fact]
        public void Add_ValueFillingOneCookie_DoesNotAddAnEmptyCookie()
        {
            //the most that fits in one cookie
            var setCookies = Add(new string('a', maxCookieSizeBytes - 256));

            Assert.Equal(["test"], setCookies.Keys);
        }

        //the last Set-Cookie for each name, as the browser would keep it, skipping the deletes that come first
        private static Dictionary<string, string> Add(string value)
        {
            var context = new DefaultHttpContext();
            new CookieManager(context).Add("test", value);

            var setCookies = new Dictionary<string, string>();
            foreach (var setCookie in context.Response.Headers.SetCookie)
            {
                var parsed = SetCookieHeaderValue.Parse(setCookie);
                if (parsed.Expires.HasValue && parsed.Expires.Value < DateTimeOffset.UtcNow)
                {
                    _ = setCookies.Remove(parsed.Name.ToString());
                    continue;
                }
                setCookies[parsed.Name.ToString()] = setCookie!;
            }
            return setCookies;
        }

        //sends the cookies back on a request and reads them
        private static string? Get(Dictionary<string, string> setCookies)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = String.Join("; ", setCookies.Values.Select(x => x.Split(';')[0]));
            return new CookieManager(context).Get("test");
        }
    }
}
