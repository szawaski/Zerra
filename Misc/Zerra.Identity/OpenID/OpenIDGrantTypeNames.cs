// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public static class OpenIDGrantTypeNames
    {
        public static string ToName(OpenIDGrantType value)
        {
            return value switch
            {
                OpenIDGrantType.authorization_code => "authorization_code",
                OpenIDGrantType.client_credentials => "client_credentials",
                _ => throw new System.ArgumentException($"Unknown {nameof(OpenIDGrantType)}: {value}")
            };
        }

        public static OpenIDGrantType? Parse(string value)
        {
            if (value is null)
                return null;
            return value switch
            {
                "authorization_code" => OpenIDGrantType.authorization_code,
                "client_credentials" => OpenIDGrantType.client_credentials,
                _ => null
            };
        }

        public static bool TryParse(string value, out OpenIDGrantType result)
        {
            switch (value)
            {
                case "authorization_code": result = OpenIDGrantType.authorization_code; return true;
                case "client_credentials": result = OpenIDGrantType.client_credentials; return true;
                default: result = default; return false;
            }
        }
    }
}
