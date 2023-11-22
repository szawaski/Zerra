// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Net;

namespace Zerra.CQRS.Network
{
    public static class IPResolver
    {
        private static readonly char[] anyhosts = new char[] { '+', '*' };
        public static IList<IPEndPoint> GetIPEndPoints(string url)
        {
            var endpoints = new List<IPEndPoint>();

            url = String.Join("anyhost", url.Split(anyhosts));

            Uri uri;
            if (!url.Contains("://"))
                uri = new Uri("any://" + url); //hacky way to make it parse without scheme.
            else
                uri = new Uri(url);

            var port = uri.Port >= 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);

            if (uri.DnsSafeHost == "anyhost")
            {
                endpoints.Add(new IPEndPoint(IPAddress.Any, port));
                endpoints.Add(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            else
            {
                try
                {
                    var ipAddresses = Dns.GetHostAddresses(uri.DnsSafeHost);
                    foreach (var ip in ipAddresses)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            endpoints.Add(new IPEndPoint(ip, port));
                    }
                }
                catch { }
            }

            return endpoints;
        }
        public static IList<IPEndPoint> GetIPEndPoints(IEnumerable<string> urls)
        {
            var endpoints = new List<IPEndPoint>();

            foreach (var item in urls)
            {
                var url = String.Join("anyhost", item.Split(anyhosts));

                Uri uri;
                if (!url.Contains("://"))
                    uri = new Uri("any://" + url); //hacky way to make it parse without scheme.
                else
                    uri = new Uri(url);

                var port = uri.Port >= 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);

                if (uri.DnsSafeHost == "anyhost")
                {
                    endpoints.Add(new IPEndPoint(IPAddress.Any, port));
                    endpoints.Add(new IPEndPoint(IPAddress.IPv6Any, port));
                }
                else
                {
                    try
                    {
                        var ipAddresses = Dns.GetHostAddresses(uri.DnsSafeHost);
                        foreach (var ip in ipAddresses)
                        {
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                                endpoints.Add(new IPEndPoint(ip, port));
                        }
                    }
                    catch { }
                }
            }

            return endpoints;
        }
    }
}