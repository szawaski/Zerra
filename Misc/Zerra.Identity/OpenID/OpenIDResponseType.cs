// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public enum OpenIDResponseType
    {
        [EnumName("code")]
        Code,
        [EnumName("id_token")]
        IdToken,
        [EnumName("code id_token")]
        Code_IdToken,
        [EnumName("token id_token")]
        Token_IdToken,
        [EnumName("token")]
        Token
    }
}
