// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.Identity.Consumers
{
    public interface IIdentityConsumer
    {
        ValueTask<IdentityHttpResponse> Login(string state = null);
        ValueTask<IdentityModel> Callback(IdentityHttpRequest context);
        ValueTask<IdentityHttpResponse> Logout(string state = null);
        ValueTask<LogoutModel> LogoutCallback(IdentityHttpRequest context);
    }
}
