// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETSTANDARD2_0
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Zerra.Serialization
{
    //.NET Framework fails a number too large for float or double, .NET Core and these return infinity
    internal static class NetStandardNumberParsing
    {
        public static bool TryParseDouble(string text, NumberStyles styles, IFormatProvider provider, out double value)
        {
            if (Double.TryParse(text, styles, provider, out value))
                return true;
            try
            {
                value = Double.Parse(text, styles, provider);
                return true;
            }
            catch (OverflowException)
            {
                value = text.TrimStart().StartsWith("-", StringComparison.Ordinal) ? Double.NegativeInfinity : Double.PositiveInfinity;
                return true;
            }
            catch (FormatException)
            {
                value = default;
                return false;
            }
        }

        public static bool TryParseSingle(string text, NumberStyles styles, IFormatProvider provider, out float value)
        {
            if (Single.TryParse(text, styles, provider, out value))
                return true;
            try
            {
                value = Single.Parse(text, styles, provider);
                return true;
            }
            catch (OverflowException)
            {
                value = text.TrimStart().StartsWith("-", StringComparison.Ordinal) ? Single.NegativeInfinity : Single.PositiveInfinity;
                return true;
            }
            catch (FormatException)
            {
                value = default;
                return false;
            }
        }

        //System.Memory's Utf8Parser fails these too, they're parsed again as text only when it does
        public static bool TryParseDouble(ReadOnlySpan<byte> utf8, out double value)
        {
            if (Utf8Parser.TryParse(utf8, out value, out var consumed) && consumed == utf8.Length)
                return true;
            return TryParseDouble(Encoding.ASCII.GetString(utf8.ToArray()), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo, out value);
        }

        public static bool TryParseSingle(ReadOnlySpan<byte> utf8, out float value)
        {
            if (Utf8Parser.TryParse(utf8, out value, out var consumed) && consumed == utf8.Length)
                return true;
            return TryParseSingle(Encoding.ASCII.GetString(utf8.ToArray()), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo, out value);
        }

        public static double ParseDouble(string text, NumberStyles styles, IFormatProvider provider)
        {
            if (!TryParseDouble(text, styles, provider, out var value))
                throw new FormatException($"Invalid number {text}");
            return value;
        }
    }
}
#endif
