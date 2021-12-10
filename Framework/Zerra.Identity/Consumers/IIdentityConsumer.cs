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
        ValueTask<IActionResult> Login(string state = null);
        ValueTask<IdentityModel> Callback(HttpContext context);
        ValueTask<IActionResult> Logout(string state = null);
        ValueTask<LogoutModel> LogoutCallback(HttpContext context);
    }
}
