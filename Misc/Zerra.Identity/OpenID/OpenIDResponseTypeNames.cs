// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public static class OpenIDResponseTypeNames
    {
        public static string ToName(OpenIDResponseType value)
        {
            return value switch
            {
                OpenIDResponseType.Code => "code",
                OpenIDResponseType.IdToken => "id_token",
                OpenIDResponseType.Code_IdToken => "code id_token",
                OpenIDResponseType.Token_IdToken => "token id_token",
                OpenIDResponseType.Token => "token",
                _ => throw new System.ArgumentException($"Unknown {nameof(OpenIDResponseType)}: {value}")
            };
        }

        public static OpenIDResponseType? Parse(string value)
        {
            if (value is null)
                return null;
            return value switch
            {
                "code" => OpenIDResponseType.Code,
                "id_token" => OpenIDResponseType.IdToken,
                "code id_token" => OpenIDResponseType.Code_IdToken,
                "token id_token" => OpenIDResponseType.Token_IdToken,
                "token" => OpenIDResponseType.Token,
                _ => null
            };
        }

        public static bool TryParse(string value, out OpenIDResponseType result)
        {
            switch (value)
            {
                case "code": result = OpenIDResponseType.Code; return true;
                case "id_token": result = OpenIDResponseType.IdToken; return true;
                case "code id_token": result = OpenIDResponseType.Code_IdToken; return true;
                case "token id_token": result = OpenIDResponseType.Token_IdToken; return true;
                case "token": result = OpenIDResponseType.Token; return true;
                default: result = default; return false;
            }
        }
    }
}
