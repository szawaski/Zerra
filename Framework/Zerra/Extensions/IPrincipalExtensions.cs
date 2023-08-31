// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

public static class IPrincipalExtensions
{
    public static bool IsInRole(this IPrincipal it, params string[] roles)
    {
        if (it == null)
            return false;
        foreach (var role in roles)
        {
            if (it.IsInRole(role))
                return true;
        }
        return false;
    }

    public static bool IsInRole<T>(this IPrincipal it, params T[] roles)
        where T : Enum
    {
        if (it == null)
            return false;
        foreach (var role in roles)
        {
            if (it.IsInRole(EnumName.GetName(role)))
                return true;
        }
        return false;
    }

    public static IPrincipal CloneClaimsPrincipal(this IPrincipal it)
    {
        if (it is ClaimsPrincipal claimsPrincipal && claimsPrincipal.Identity is ClaimsIdentity claimsIdentity)
        {
            return new ClaimsPrincipal(claimsPrincipal.Identities.Select(x => x.Clone()));
        }
        return null;
    }
}