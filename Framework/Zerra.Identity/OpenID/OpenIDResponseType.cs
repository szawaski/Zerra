// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public enum OpenIDResponseType
    {
        [EnumName("code")]
        code,
        [EnumName("id_token")]
        id_token,
        [EnumName("code id_token")]
        code_id_token,
        [EnumName("token id_token")]
        token_id_token,
        [EnumName("token")]
        token
    }
}
