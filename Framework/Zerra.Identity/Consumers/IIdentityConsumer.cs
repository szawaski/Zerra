// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Zerra.Identity.Consumers
{
    public interface IIdentityConsumer
    {
        ValueTask<IActionResult> Login(string serviceProvider, string state);
        ValueTask<IdentityModel> Callback(HttpContext context, string serviceProvider);
        ValueTask<IActionResult> Logout(string serviceProvider, string state);
        ValueTask<LogoutModel> LogoutCallback(HttpContext context, string serviceProvider);
    }
}
