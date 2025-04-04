// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Zerra.TestDev
{
    public struct LookupReference
    {
        public ulong Key;
        public byte[] Name;
        public string Value;
    }
    public static class LookupTest
    {
        public static void Test()
        {
            var size = 9;
            var itterations = 10000000;
            Stopwatch timer;

            var keys = new List<string>();
            var array = new KeyValuePair<string, string>[size];
            var dictionary = new Dictionary<string, string>();
            var table = new LookupReference[size];

            for (var i = 0; i < 9; i++)
            {
                var item = new KeyValuePair<string, string>($"P{Guid.NewGuid().ToString()}", Guid.NewGuid().ToString());
                keys.Add(item.Key);
                array[i] = item;
                dictionary.Add(item.Key, item.Value);
                table[i] = new LookupReference() { Key = GetKey(Encoding.UTF8.GetBytes(item.Key)), Name = Encoding.UTF8.GetBytes(item.Key), Value = item.Value };
            }

            int propertyIndex = 0;
            int nextPropertyIndex = 0;

            for (var k = 0; k < keys.Count; k++)
            {
                var keyToFind = keys[k];
                var keyToFindBytes = Encoding.UTF8.GetBytes(keyToFind);

                Console.WriteLine();

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    string? result = null;
                    foreach (var item in array)
                    {
                        if (StringComparer.Ordinal.Equals(item.Key, keyToFind))
                        {
                            result = item.Value;
                            break;
                        }
                    }
                }
                timer.Stop();
                Console.WriteLine($"Key {k}, Array Enumerable {timer.ElapsedMilliseconds}ms");

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    string? result = null;
                    for (var j = 0; j < array.Length; j++)
                    {
                        var item = array[j];
                        if (StringComparer.Ordinal.Equals(item.Key, keyToFind))
                        {
                            result = item.Value;
                            break;
                        }
                    }
                }
                timer.Stop();
                Console.WriteLine($"Key {k}, Array ForLoop {timer.ElapsedMilliseconds}ms");

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    _ = dictionary.TryGetValue(keyToFind, out var result);
                }
                timer.Stop();
                Console.WriteLine($"Key {k}, Dictionary {timer.ElapsedMilliseconds}ms");

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    int iForward = Math.Min(propertyIndex, table.Length);
                    int iBackward = iForward - 1;
                    LookupReference item;

                    var key = GetKey(keyToFindBytes);

                    while (true)
                    {
                        if (iForward < table.Length)
                        {
                            item = table[iForward];
                            if (IsPropertyRefEqual(item, keyToFindBytes, key))
                            {
                                nextPropertyIndex = iForward;
                                break;
                            }

                            ++iForward;

                            if (iBackward >= 0)
                            {
                                item = table[iBackward];
                                if (IsPropertyRefEqual(item, keyToFindBytes, key))
                                {
                                    nextPropertyIndex = iBackward;
                                    break;
                                }

                                --iBackward;
                            }
                        }
                        else if (iBackward >= 0)
                        {
                            item = table[iBackward];
                            if (IsPropertyRefEqual(item, keyToFindBytes, key))
                            {
                                nextPropertyIndex = iBackward;
                                break;
                            }

                            --iBackward;
                        }
                        else
                        {
                            // Property was not found.
                            break;
                        }
                    }
                }
                propertyIndex = nextPropertyIndex;

                timer.Stop();
                Console.WriteLine($"Key {k}, Table {timer.ElapsedMilliseconds}ms");
            }

            Console.WriteLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetKey(ReadOnlySpan<byte> name)
        {
            ulong key;

            ref byte reference = ref MemoryMarshal.GetReference(name);
            int length = name.Length;

            if (length > 7)
            {
                key = Unsafe.ReadUnaligned<ulong>(ref reference) & 0x00ffffffffffffffL;
                key |= (ulong)Math.Min(length, 0xff) << 56;
            }
            else
            {
                key =
                    length > 5 ? Unsafe.ReadUnaligned<uint>(ref reference) | (ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32 :
                    length > 3 ? Unsafe.ReadUnaligned<uint>(ref reference) :
                    length > 1 ? Unsafe.ReadUnaligned<ushort>(ref reference) : 0UL;
                key |= (ulong)length << 56;

                if ((length & 1) != 0)
                {
                    var offset = length - 1;
                    key |= (ulong)Unsafe.Add(ref reference, offset) << (offset * 8);
                }
            }

            return key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPropertyRefEqual(in LookupReference propertyRef, ReadOnlySpan<byte> propertyName, ulong key)
        {
            if (key == propertyRef.Key)
            {
                if (propertyName.Length <= 7 || propertyName.SequenceEqual(propertyRef.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
