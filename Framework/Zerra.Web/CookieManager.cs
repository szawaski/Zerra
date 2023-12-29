// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;

namespace Zerra.Web
{
    public sealed class CookieManager
    {
        private readonly HttpContext context;
        private readonly IDataProtectionProvider? dataProtectionProvider;

        public CookieManager(HttpContext context)
        {
            this.context = context;
        }
        public CookieManager(HttpContext context, IDataProtectionProvider dataProtectionProvider)
        {
            this.context = context;
            this.dataProtectionProvider = dataProtectionProvider;
        }

        private const int maxCookieSizeBytes = 4096;
        private const int maxCookiesPerDomain = 20;

        public void Add(string name, string value, TimeSpan? maxAge = null, SameSiteMode sameSite = SameSiteMode.Strict, bool httpOnly = false, bool secure = false)
        {
            PersistCookie(name, value, maxAge, sameSite, httpOnly, secure);
        }

        public void AddSecure(string name, string value, TimeSpan? maxAge = null, SameSiteMode sameSite = SameSiteMode.Strict, bool httpOnly = true, bool secure = true)
        {
            var encryptedValue = Encrypt(value);
            PersistCookie(name, encryptedValue, maxAge, sameSite, httpOnly, secure);
        }

        public string? Get(string name)
        {
            return ReadCookie(name);
        }
        public string? GetSecure(string name)
        {
            var value = ReadCookie(name);
            if (value != null)
            {
                var decryptedValue = Decrypt(value);
                return decryptedValue;
            }
            return null;
        }

        public void Remove(string name, SameSiteMode sameSite = SameSiteMode.Strict, bool httpOnly = true, bool secure = true)
        {
            DeleteCookie(name, sameSite, httpOnly, secure);
        }

        private string? Encrypt(string? value)
        {
            if (dataProtectionProvider == null)
                throw new Exception("CookieManager created without an IDataProtectionProvider");

            if (String.IsNullOrWhiteSpace(value))
                return null;

            var provider = dataProtectionProvider.CreateProtector("Cookies");

            var encryptedValue = provider.Protect(value);

            return encryptedValue;
        }
        private string? Decrypt(string? encryptedValue)
        {
            if (dataProtectionProvider == null)
                throw new Exception("CookieManager created without an IDataProtectionProvider");

            if (String.IsNullOrWhiteSpace(encryptedValue))
                return null;

            var protector = dataProtectionProvider.CreateProtector("Cookies");

            try
            {
                var value = protector.Unprotect(encryptedValue);
                return value;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return null;
            }
        }

        private void PersistCookie(string name, string? value, TimeSpan? maxAge, SameSiteMode sameSite, bool httpOnly, bool secure)
        {
            value ??= String.Empty;
            var cookieSeriesCount = value.Length * 2 / maxCookieSizeBytes + 1;

            if (cookieSeriesCount > maxCookiesPerDomain)
                throw new Exception("Cookie too large");

            int x;
            for (x = 0; x < cookieSeriesCount; x++)
            {
                var seriesName = x == 0 ? name : $"{name}-{x}";
                var seriesValue = value.Substring(x * maxCookieSizeBytes / 2, Math.Min(maxCookieSizeBytes / 2, value.Length - x * maxCookieSizeBytes / 2));

                context.Response.Cookies.Delete(seriesName);
                context.Response.Cookies.Append(seriesName, seriesValue, new CookieOptions()
                {
                    Path = "/",
                    SameSite = sameSite,
                    Secure = secure,
                    HttpOnly = httpOnly,
                    MaxAge = maxAge,
                    Expires = maxAge.HasValue ? DateTimeOffset.UtcNow.Add(maxAge.Value) : (DateTimeOffset?)null
                });
            }
            for (var y = x; y < maxCookiesPerDomain; y++)
            {
                var seriesName = y == 0 ? name : $"{name}-{y}";
                if (context.Request.Cookies.Keys.Contains(seriesName))
                {
                    context.Response.Cookies.Delete(seriesName);
                    context.Response.Cookies.Append(seriesName, String.Empty, new CookieOptions()
                    {
                        Path = "/",
                        MaxAge = new TimeSpan(0),
                        Expires = DateTimeOffset.MinValue
                    });
                }
            }
        }
        private string? ReadCookie(string name)
        {
            var sb = new StringBuilder();
            for (var x = 0; x < maxCookiesPerDomain; x++)
            {
                var seriesName = x == 0 ? name : $"{name}-{x}";
                if (!context.Request.Cookies.Keys.Contains(seriesName))
                    break;
                var cookie = context.Request.Cookies[seriesName];
                _ = sb.Append(cookie);
            }

            if (sb.Length == 0)
                return null;

            return sb.ToString();
        }
        private void DeleteCookie(string name, SameSiteMode sameSite, bool httpOnly, bool secure)
        {
            for (var x = 0; x < maxCookiesPerDomain; x++)
            {
                var seriesName = x == 0 ? name : $"{name}-{x}";
                if (context.Request.Cookies.Keys.Contains(seriesName))
                {
                    context.Response.Cookies.Delete(seriesName);
                    context.Response.Cookies.Append(seriesName, String.Empty, new CookieOptions()
                    {
                        Path = "/",
                        MaxAge = new TimeSpan(0),
                        Expires = DateTimeOffset.MinValue,
                        SameSite = sameSite,
                        HttpOnly = httpOnly,
                        Secure = secure
                    });
                }
            }
        }
    }
}