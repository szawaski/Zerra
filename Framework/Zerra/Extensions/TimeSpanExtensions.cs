// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

public static class TimeSpanExtensions
{
    public static string ToStringTime(this TimeSpan it, string formatExpression, bool removeZeros = false)
    {
        if (String.IsNullOrWhiteSpace(formatExpression))
            throw new ArgumentException("formatExpression is empty");

        Span<char> buffer = stackalloc char[32];
        var writer = new CharWriter(buffer);
        var chars = formatExpression.AsSpan();
        var hasWrittenOffset = 0;

        if (it.Ticks < 0)
        {
            writer.Write('-');
            it = TimeSpan.FromTicks(-it.Ticks);
            hasWrittenOffset = -1;
        }

        var startIndex = 0;
        var lastChar = chars[0];
        for (var i = 1; i < chars.Length; i++)
        {
            var c = chars[i];
            if (i == chars.Length - 1 || c != lastChar)
            {
                var token = chars.Slice(startIndex, i == chars.Length - 1 ? chars.Length - startIndex : i - startIndex);
                startIndex = i;
                lastChar = c;

                int value;
                switch (token[0])
                {
                    case 'D': value = (int)it.TotalDays; break;
                    case 'd': value = it.Days; break;
                    case 'H': value = (int)it.TotalHours; break;
                    case 'h': value = it.Hours; break;
                    case 'M': value = (int)it.TotalMinutes; break;
                    case 'm': value = it.Minutes; break;
                    case 'S': value = (int)it.TotalSeconds; break;
                    case 's': value = it.Seconds; break;
                    case 'F': value = (int)it.TotalMilliseconds; break;
                    case 'f': value = it.Milliseconds; break;
                    case 'T': value = it.Hours > 12 ? (it.Hours - 12) : it.Hours; break;
                    case 't':
                        if (token.Length == 1)
                            writer.Write(it.Hours >= 12 ? "P" : "A");
                        else
                            writer.Write(it.Hours >= 12 ? "PM" : "AM");
                        continue;
                    default:
                        if (!removeZeros || writer.Length + hasWrittenOffset > 0)
                            writer.Write(token.ToArray(), 0, token.Length);
                        continue;
                }

                if (value == 0 && removeZeros && writer.Length + hasWrittenOffset == 0 && i < chars.Length - 1)
                    continue;

                if (!removeZeros || writer.Length + hasWrittenOffset > 0)
                {
                    int digits;
                    if (value < 10)
                        digits = 1;
                    else if (value < 100)
                        digits = 2;
                    else if (value < 1000)
                        digits = 3;
                    else if (value < 10000)
                        digits = 4;
                    else if (value < 100000)
                        digits = 5;
                    else if (value < 1000000)
                        digits = 6;
                    else if (value < 10000000)
                        digits = 7;
                    else if (value < 100000000)
                        digits = 8;
                    else if (value < 1000000000)
                        digits = 9;
                    else
                        digits = 10;

                    while (digits < token.Length)
                    {
                        writer.Write('0');
                        digits++;
                    }
                }
                writer.Write(value);
            }
        }

        return writer.ToString();
    }
}