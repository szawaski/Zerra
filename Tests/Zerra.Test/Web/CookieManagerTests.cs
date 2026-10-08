// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.DataProtection;
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

        [Fact]
        public void Add_SetsOptions()
        {
            var context = new DefaultHttpContext();
            new CookieManager(context).Add("test", "value", TimeSpan.FromMinutes(5), Microsoft.AspNetCore.Http.SameSiteMode.Lax, httpOnly: true, secure: true);

            var cookie = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray()).Last();
            Assert.Equal("value", cookie.Value.ToString());
            Assert.Equal("/", cookie.Path.ToString());
            Assert.Equal(TimeSpan.FromMinutes(5), cookie.MaxAge);
            Assert.True(cookie.Expires > DateTimeOffset.UtcNow);
            Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, cookie.SameSite);
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
        }

        [Fact]
        public void Add_ShorterValue_ExpiresLeftoverParts()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = "test=a; test-1=b; test-2=c; other=d";

            new CookieManager(context).Add("test", "value");

            var cookies = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray());
            Assert.Equal("value", cookies.Last(x => x.Name == "test").Value.ToString());
            Assert.True(cookies.Last(x => x.Name == "test-1").Expires < DateTimeOffset.UtcNow);
            Assert.True(cookies.Last(x => x.Name == "test-2").Expires < DateTimeOffset.UtcNow);
            Assert.DoesNotContain(cookies, x => x.Name == "test-3" || x.Name == "other");
        }

        [Fact]
        public void Add_TooLarge_Throws()
        {
            var context = new DefaultHttpContext();
            _ = Assert.Throws<Exception>(() => new CookieManager(context).Add("test", new string('a', 21 * maxCookieSizeBytes)));
        }

        [Fact]
        public void Get_Missing_ReturnsNull()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = "other=value";
            var manager = new CookieManager(context, new EphemeralDataProtectionProvider());
            Assert.Null(manager.Get("test"));
            Assert.Null(manager.GetSecure("test"));
        }

        [Fact]
        public void Remove_ExpiresEveryPart()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = "test=a; test-1=b; other=c";

            new CookieManager(context).Remove("test");

            var cookies = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray());
            Assert.True(cookies.Last(x => x.Name == "test").Expires < DateTimeOffset.UtcNow);
            Assert.True(cookies.Last(x => x.Name == "test-1").Expires < DateTimeOffset.UtcNow);
            Assert.DoesNotContain(cookies, x => x.Name == "other");
        }

        [Fact]
        public void AddSecure_RoundTripsEncrypted()
        {
            var provider = new EphemeralDataProtectionProvider();
            var value = new string('a', 10_000);

            var context = new DefaultHttpContext();
            new CookieManager(context, provider).AddSecure("test", value);

            var cookies = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray()).Where(x => x.Expires is null || x.Expires > DateTimeOffset.UtcNow).ToArray();
            Assert.True(cookies.Length > 1);
            Assert.All(cookies, x => Assert.True(x.HttpOnly && x.Secure));
            Assert.DoesNotContain(cookies, x => x.Value.ToString().Contains("aaaa"));

            var requestContext = new DefaultHttpContext();
            requestContext.Request.Headers.Cookie = String.Join("; ", cookies.Select(x => $"{x.Name}={x.Value}"));
            var manager = new CookieManager(requestContext, provider);
            Assert.Equal(value, manager.GetSecure("test"));
            Assert.NotEqual(value, manager.Get("test"));
        }

        [Fact]
        public void Add_ThreeByteCharacters_RoundTripsAcrossParts()
        {
            //each part is measured URL escaped, a three byte character is nine
            var value = String.Concat(Enumerable.Repeat("中a€é😀", 2_000));

            var context = new DefaultHttpContext();
            new CookieManager(context).Add("test", value);

            var cookies = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray()).Where(x => x.Expires is null || x.Expires > DateTimeOffset.UtcNow).ToArray();
            Assert.True(cookies.Length > 1);
            Assert.All(cookies, x => Assert.True(x.Value.Length <= maxCookieSizeBytes));

            var requestContext = new DefaultHttpContext();
            requestContext.Request.Headers.Cookie = String.Join("; ", cookies.Select(x => $"{x.Name}={x.Value}"));
            Assert.Equal(value, new CookieManager(requestContext).Get("test"));
        }

        [Fact]
        public void Secure_EmptyValue_IsNull()
        {
            var provider = new EphemeralDataProtectionProvider();
            var context = new DefaultHttpContext();
            new CookieManager(context, provider).AddSecure("test", "");
            var cookie = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray()).Last(x => x.Name == "test");
            Assert.Equal("", cookie.Value.ToString());

            var requestContext = new DefaultHttpContext();
            requestContext.Request.Headers.Cookie = "test=";
            Assert.Null(new CookieManager(requestContext, provider).GetSecure("test"));
        }

        [Fact]
        public void GetSecure_TamperedOrOtherKey_ReturnsNull()
        {
            var context = new DefaultHttpContext();
            new CookieManager(context, new EphemeralDataProtectionProvider()).AddSecure("test", "value");
            var cookie = SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.ToArray()).Last();

            var tampered = new DefaultHttpContext();
            tampered.Request.Headers.Cookie = $"test={cookie.Value}x";
            Assert.Null(new CookieManager(tampered, new EphemeralDataProtectionProvider()).GetSecure("test"));

            var otherKey = new DefaultHttpContext();
            otherKey.Request.Headers.Cookie = $"test={cookie.Value}";
            Assert.Null(new CookieManager(otherKey, new EphemeralDataProtectionProvider()).GetSecure("test"));
        }

        [Fact]
        public void Secure_WithoutDataProtection_Throws()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = "test=value";
            var manager = new CookieManager(context);
            _ = Assert.Throws<Exception>(() => manager.AddSecure("test", "value"));
            _ = Assert.Throws<Exception>(() => manager.GetSecure("test"));
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
