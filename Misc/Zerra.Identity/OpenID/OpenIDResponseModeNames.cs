// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public static class OpenIDResponseModeNames
    {
        public static string ToName(OpenIDResponseMode value)
        {
            return value switch
            {
                OpenIDResponseMode.form_post => "form_post",
                OpenIDResponseMode.query => "query",
                OpenIDResponseMode.fragment => "fragment",
                _ => throw new System.ArgumentException($"Unknown {nameof(OpenIDResponseMode)}: {value}")
            };
        }

        public static OpenIDResponseMode? Parse(string value)
        {
            if (value is null)
                return null;
            return value switch
            {
                "form_post" => OpenIDResponseMode.form_post,
                "query" => OpenIDResponseMode.query,
                "fragment" => OpenIDResponseMode.fragment,
                _ => null
            };
        }

        public static bool TryParse(string value, out OpenIDResponseMode result)
        {
            switch (value)
            {
                case "form_post": result = OpenIDResponseMode.form_post; return true;
                case "query": result = OpenIDResponseMode.query; return true;
                case "fragment": result = OpenIDResponseMode.fragment; return true;
                default: result = default; return false;
            }
        }
    }
}
