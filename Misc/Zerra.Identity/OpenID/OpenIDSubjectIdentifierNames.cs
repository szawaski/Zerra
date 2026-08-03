// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity.OpenID
{
    public static class OpenIDSubjectIdentifierNames
    {
        public static string ToName(OpenIDSubjectIdentifier value)
        {
            return value switch
            {
                OpenIDSubjectIdentifier.public_ => "public",
                OpenIDSubjectIdentifier.pairwise => "pairwise",
                _ => throw new System.ArgumentException($"Unknown {nameof(OpenIDSubjectIdentifier)}: {value}")
            };
        }

        public static OpenIDSubjectIdentifier? Parse(string value)
        {
            if (value is null)
                return null;
            return value switch
            {
                "public" => OpenIDSubjectIdentifier.public_,
                "pairwise" => OpenIDSubjectIdentifier.pairwise,
                _ => null
            };
        }

        public static bool TryParse(string value, out OpenIDSubjectIdentifier result)
        {
            switch (value)
            {
                case "public": result = OpenIDSubjectIdentifier.public_; return true;
                case "pairwise": result = OpenIDSubjectIdentifier.pairwise; return true;
                default: result = default; return false;
            }
        }
    }
}
