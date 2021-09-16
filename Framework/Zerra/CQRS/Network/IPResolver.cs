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
        public static IList<IPEndPoint> GetIPEndPoints(string urls, int defaultPort)
        {
            var endpoints = new List<IPEndPoint>();

            urls = urls.Replace("+", "anyhost");
            var split = urls.Split(";", StringSplitOptions.RemoveEmptyEntries);

            foreach (var url in split)
            {
                Uri uri;
                if (!url.Contains("://"))
                    uri = new Uri("any://" + url); //hacky way to make it parse without scheme.
                else
                    uri = new Uri(url);

                var port = uri.Port >= 0 ? uri.Port : defaultPort;

                if (uri.DnsSafeHost == "anyhost")
                {
                    endpoints.Add(new IPEndPoint(IPAddress.Any, port));
                    endpoints.Add(new IPEndPoint(IPAddress.IPv6Any, port));
                }
                else
                {
                    var ipAddresses = Dns.GetHostAddresses(uri.DnsSafeHost);
                    foreach (var ip in ipAddresses)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            endpoints.Add(new IPEndPoint(ip, port));
                    }
                }

            }

            return endpoints;
        }
    }
}