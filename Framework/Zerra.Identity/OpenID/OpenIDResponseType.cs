// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public enum OpenIDResponseType
    {
        [DisplayText("code")]
        code,
        [DisplayText("id_token")]
        id_token,
        [DisplayText("code id_token")]
        code_id_token,
        [DisplayText("token id_token")]
        token_id_token,
        [DisplayText("token")]
        token
    }
}
