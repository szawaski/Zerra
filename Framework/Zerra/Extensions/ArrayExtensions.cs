// Copyright © KaKush LLC
// Licensed to you under the MIT license

using System;

public static class ArrayExtensions
{
    public static T[] ConcatToArray<T>(this T[] it, params T[] array) { return ConcatToArray(it, it.Length, array); }
    public static T[] ConcatToArray<T>(this T[] it, int start, params T[] array)
    {
        if (start < 0) throw new ArgumentException("start cannot be less than zero");
        if (start > it.Length) throw new ArgumentException("start cannot be greater than the array length");

        var newArray = Array.CreateInstance(typeof(T), it.Length + array.Length);
        if (start > 0)
            Array.Copy(it, 0, newArray, 0, start);
        Array.Copy(array, 0, newArray, start, array.Length);
        if (start < it.Length)
            Array.Copy(it, start, newArray, start + array.Length, it.Length - start);
        return (T[])newArray;
    }

    public static unsafe bool BytesEquals(this byte[] strA, byte[] strB)
    {
        int length = strA.Length;
        if (length != strB.Length)
        {
            return false;
        }
        fixed (byte* str = strA)
        {
            byte* chPtr = str;
            fixed (byte* str2 = strB)
            {
                byte* chPtr2 = str2;
                byte* chPtr3 = chPtr;
                byte* chPtr4 = chPtr2;
                while (length >= 10)
                {
                    if ((((*(((int*)chPtr3)) != *(((int*)chPtr4))) || (*(((int*)(chPtr3 + 2))) != *(((int*)(chPtr4 + 2))))) || ((*(((int*)(chPtr3 + 4))) != *(((int*)(chPtr4 + 4)))) || (*(((int*)(chPtr3 + 6))) != *(((int*)(chPtr4 + 6)))))) || (*(((int*)(chPtr3 + 8))) != *(((int*)(chPtr4 + 8)))))
                    {
                        break;
                    }
                    chPtr3 += 10;
                    chPtr4 += 10;
                    length -= 10;
                }
                while (length > 0)
                {
                    if (*(((int*)chPtr3)) != *(((int*)chPtr4)))
                    {
                        break;
                    }
                    chPtr3 += 2;
                    chPtr4 += 2;
                    length -= 2;
                }
                return (length <= 0);
            }
        }
    }
}