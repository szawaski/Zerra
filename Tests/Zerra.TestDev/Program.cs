// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Zerra.Reflection;

namespace Zerra.TestDev
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            var timer = Stopwatch.StartNew();

            TestTaskRun.Test().GetAwaiter().GetResult();

            //UtfTest();

            //TestMe();
            //TestMe2();
            //TestMe3();
            //JsonSerializerTest.CompareTestSpeed();
            //ByteSerializerTest.CompareTestSpeed();

            //TestMemory.TryFinallyOrDisposed();
            //InlineTest.Test();

            //JsonSerializerTest.TestSpeed().GetAwaiter().GetResult();
            //ByteSerializerTest.TestSpeed();

            //TestMath.Test();

            ////MSILTest.Test();

            //T4DataModelsTest.Test();
            ////T4DataModelsTest.Test();
            //T4JavaScriptTest.Test();
            //T4TypeScriptTest.Test();

            ////TestDiscovery.Test();
            ////TestLinqValueExtract.TestGetIDs();
            ////TestLinq.Test();

            //TestRepoSpeed.Test();


            Console.WriteLine($"Done {timer.ElapsedMilliseconds:n0}ms");
            _ = Console.ReadLine();
        }

        private static void UtfTest()
        {
            var chars = new char[(int)char.MaxValue];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)i;

            var charsFromBytes = new char[(int)byte.MaxValue];
            for (byte i = 0; i < charsFromBytes.Length; i++)
                charsFromBytes[i] = (char)i;

            var utf16 = new Dictionary<char, byte[]>();
            var utf8 = new Dictionary<char, byte[]>();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                utf16.Add(c, Encoding.Unicode.GetBytes([c]));
                utf8.Add(c, Encoding.UTF8.GetBytes([c]));
            }
            var max16 = utf16.Values.Select(x => x.Length).Max();
            var max8 = utf8.Values.Select(x => x.Length).Max();

            var oneByte = utf8.Where(x => x.Value.Length == 1).ToDictionary(x => x.Key, x => x.Value[0]);
            var oneByteMin = oneByte.Values.Min();
            var oneByteMax = oneByte.Values.Max();
            var oneByteRange = oneByte.Values.Distinct().ToArray();

            var twoByte = utf8.Where(x => x.Value.Length == 2).ToDictionary(x => x.Key, x => x.Value[0]);
            var twoByteView = utf8.Where(x => x.Value.Length == 2).ToDictionary(x => x.Key, x => $"[{x.Value[0]},{x.Value[1]}]");
            var twoByteMin = twoByte.Values.Min();
            var twoByteMax = twoByte.Values.Max();
            var twoByteRange = twoByte.Values.Distinct().ToArray();

            var threeByte = utf8.Where(x => x.Value.Length == 3).ToDictionary(x => x.Key, x => x.Value[0]);
            var threeByteView = utf8.Where(x => x.Value.Length == 3).ToDictionary(x => x.Key, x => $"[{x.Value[0]},{x.Value[1]},{x.Value[2]}]");
            var threeByteMin = threeByte.Values.Min();
            var threeByteMax = threeByte.Values.Max();
            var threeByteRange = threeByte.Values.Distinct().ToArray();
        }

        private static void TestMe()
        {
            Stopwatch timer;
            var type1 = typeof(Zerra.Test.TypesAllModel);
            var typeDetail1 = TypeAnalyzer<Zerra.Test.TypesAllModel>.GetTypeDetail();
            var type2 = typeof(Zerra.Test.TypesAllAsStringsModel);
            var typeDetail2 = TypeAnalyzer<Zerra.Test.TypesAllAsStringsModel>.GetTypeDetail();

            timer = Stopwatch.StartNew();
            foreach (var member in type1.GetProperties())
            {
                var memberDetail = typeDetail1.GetMember(member.Name);
            }
            timer.Stop();
            Console.WriteLine($"Individual {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            var all = typeDetail2.MemberDetails;
            foreach (var member in type2.GetProperties())
            {
                var memberDetail = typeDetail2.GetMember(member.Name);
            }
            timer.Stop();
            Console.WriteLine($"Bulk Load {timer.ElapsedMilliseconds}ms");
        }

        private static unsafe void TestMe2()
        {
            const byte tByte = (byte)'t';
            const byte rByte = (byte)'r';
            const byte uByte = (byte)'u';
            const byte eByte = (byte)'e';
            var trueBytes = Encoding.UTF8.GetBytes("trueeee");

            Stopwatch timer;
            var testerString = "I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.I am a string of things for strings. The string's chars are here.";
            var testerCharsArray = testerString.ToCharArray();
            var testerCharsSpan = testerString.AsSpan();
            var testerBytesArray = Encoding.UTF8.GetBytes(testerString);
            var testerBytesSpan = Encoding.UTF8.GetBytes(testerString).AsSpan();

            Span<char> charBuffer = new char[8192];
            Span<byte> byteBuffer = new byte[8192 * 2];

            var itterations = 10000000;
            string result;

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerString)
                fixed (char* pBuffer = charBuffer)
                {
                    for (var p = 0; p < testerString.Length; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
            }
            result = charBuffer[..testerString.Length].ToString();
            timer.Stop();
            Console.WriteLine($"String Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerString)
                fixed (char* pBuffer = charBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, testerString.Length * 2);
                }
            }
            result = charBuffer[..testerString.Length].ToString();
            timer.Stop();
            Console.WriteLine($"String MemoryCopy {timer.ElapsedMilliseconds}ms");


            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerCharsArray)
                fixed (char* pBuffer = charBuffer)
                {
                    for (var p = 0; p < testerCharsArray.Length; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
            }
            result = charBuffer[..testerCharsArray.Length].ToString();
            timer.Stop();
            Console.WriteLine($"CharsArray Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerCharsArray)
                fixed (char* pBuffer = charBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, testerCharsArray.Length * 2);
                }
            }
            result = charBuffer[..testerCharsArray.Length].ToString();
            timer.Stop();
            Console.WriteLine($"CharsArray MemoryCopy {timer.ElapsedMilliseconds}ms");


            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerCharsSpan)
                fixed (char* pBuffer = charBuffer)
                {
                    for (var p = 0; p < testerCharsSpan.Length; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
            }
            result = charBuffer[..testerCharsSpan.Length].ToString();
            timer.Stop();
            Console.WriteLine($"CharsSpan Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (char* pSource = testerCharsSpan)
                fixed (char* pBuffer = charBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, testerCharsSpan.Length * 2);
                }
            }
            result = charBuffer[..testerCharsSpan.Length].ToString();
            timer.Stop();
            Console.WriteLine($"CharsSpan MemoryCopy {timer.ElapsedMilliseconds}ms");


            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (byte* pSource = testerBytesArray)
                fixed (byte* pBuffer = byteBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, testerBytesArray.Length);
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesArray.Length]);
            timer.Stop();
            Console.WriteLine($"BytesArray MemoryCopy {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (byte* pSource = testerBytesArray)
                fixed (byte* pBuffer = byteBuffer)
                {
                    for (var p = 0; p < testerBytesArray.Length; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesArray.Length]);
            timer.Stop();
            Console.WriteLine($"BytesArray Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                for (var p = 0; p < testerBytesArray.Length; p++)
                {
                    byteBuffer[p] = testerBytesArray[p];
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesArray.Length]);
            timer.Stop();
            Console.WriteLine($"BytesArray Regular {timer.ElapsedMilliseconds}ms");


            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (byte* pSource = testerBytesSpan)
                fixed (byte* pBuffer = byteBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, testerBytesSpan.Length);
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesSpan.Length]);
            timer.Stop();
            Console.WriteLine($"BytesSpan MemoryCopy {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (byte* pSource = testerBytesSpan)
                fixed (byte* pBuffer = byteBuffer)
                {
                    for (var p = 0; p < testerBytesSpan.Length; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesSpan.Length]);
            timer.Stop();
            Console.WriteLine($"BytesSpan Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                for (var p = 0; p < testerBytesSpan.Length; p++)
                {
                    byteBuffer[p] = testerBytesSpan[p];
                }
            }
            result = Encoding.UTF8.GetString(byteBuffer[..testerBytesSpan.Length]);
            timer.Stop();
            Console.WriteLine($"BytesSpan Regular {timer.ElapsedMilliseconds}ms");


            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                fixed (byte* pSource = trueBytes)
                fixed (byte* pBuffer = byteBuffer)
                {
                    Buffer.MemoryCopy(pSource, pBuffer, byteBuffer.Length, trueBytes.Length);
                }
            }
            timer.Stop();
            Console.WriteLine($"True MemoryCopy {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                var position = 0;
                fixed (byte* pBuffer = byteBuffer)
                {
                    pBuffer[position++] = tByte;
                    pBuffer[position++] = rByte;
                    pBuffer[position++] = uByte;
                    pBuffer[position++] = eByte;
                    pBuffer[position++] = eByte;
                    pBuffer[position++] = eByte;
                    pBuffer[position++] = eByte;
                    pBuffer[position++] = eByte;
                }
            }
            timer.Stop();
            Console.WriteLine($"True Fixed {timer.ElapsedMilliseconds}ms");

            timer = Stopwatch.StartNew();
            for (var i = 0; i < itterations; i++)
            {
                var position = 0;
                byteBuffer[position++] = tByte;
                byteBuffer[position++] = rByte;
                byteBuffer[position++] = uByte;
                byteBuffer[position++] = eByte;
                byteBuffer[position++] = eByte;
                byteBuffer[position++] = eByte;
                byteBuffer[position++] = eByte;
                byteBuffer[position++] = eByte;
            }
            timer.Stop();
            Console.WriteLine($"True Span {timer.ElapsedMilliseconds}ms");
        }

        private static void TestMe3()
        {
            const int itterations = 20000000;
            Stopwatch timer;
            for (var a = 0; a < 10; a++)
            {
                var chars = new char[a];
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = i.ToString()[0];
                var span = chars.AsSpan();

                Console.WriteLine();

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    _ = GetHashCode(span);
                }
                timer.Stop();
                Console.WriteLine($"GetHashCode {a} {timer.ElapsedMilliseconds}ms");

                timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
                    _ = GetHashCode2(span);
                }
                timer.Stop();
                Console.WriteLine($"GetHashCode2 {a} {timer.ElapsedMilliseconds}ms");
            }

            Console.WriteLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetHashCode(ReadOnlySpan<char> name)
        {
            ref byte reference = ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(name));
            var length = name.Length * 2;

            ulong code;
            //even cases not possible, most chars have 2nd byte as 0 so skip those
            switch (length)
            {
                case > 14:
                    //take 4 bytes from front and 3 bytes from end
                    // 00000000_########_########_########_########_########_########_########
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, length - 6) << 32) | ((ulong)Unsafe.Add(ref reference, length - 4) << 40) | ((ulong)Unsafe.Add(ref reference, length - 2) << 48);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)Math.Min(length, 255) << 56;
                    return code;
                case 14:
                    // 00000000_########_########_########_########_########_########_########
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32) | ((ulong)Unsafe.Add(ref reference, 10) << 40) | ((ulong)Unsafe.Add(ref reference, 12) << 48);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)Math.Min(length, 255) << 56;
                    return code;
                case 12:
                    // 00000000_00000000_########_########_########_########_########_########
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32) | ((ulong)Unsafe.Add(ref reference, 10) << 40);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)Math.Min(length, 255) << 56;
                    return code;
                case 10:
                    // 00000000_00000000_00000000_########_########_########_########_########
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24) | ((ulong)Unsafe.Add(ref reference, 8) << 32);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)Math.Min(length, 255) << 56;
                    return code;
                case 8:
                    // 00000000_00000000_00000000_00000000_########_########_########_########
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16) | ((ulong)Unsafe.Add(ref reference, 6) << 24);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)Math.Min(length, 255) << 56;
                    return code;
                case 6:
                    // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000 | 00000000_00000000_00000000_00000000_00000000_########_00000000_00000000
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8) | ((ulong)Unsafe.Add(ref reference, 4) << 16);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)length << 56;
                    return code;
                case 4:
                    // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000
                    code = (ulong)reference | ((ulong)Unsafe.Add(ref reference, 2) << 8);

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)length << 56;
                    return code;
                case 2:
                    // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_########
                    code = (ulong)reference;

                    // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                    code |= (ulong)length << 56;
                    return code;
                default:
                    code = 0UL;
                    return code;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static ulong GetHashCode2(ReadOnlySpan<char> name)
        {
            var length = name.Length * 2;
            ulong code;
            fixed (void* pVoid = name)
            {
                var p = (byte*)pVoid;

                //even cases not possible, most chars have 2nd byte as 0 so skip those
                switch (length)
                {
                    case > 14:
                        //take 4 bytes from front and 3 bytes from end
                        // 00000000_########_########_########_########_########_########_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[6]) << 24) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[length - 6]) << 32) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[length - 4]) << 40) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[length - 2]) << 48);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 14:
                        // 00000000_########_########_########_########_########_########_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[6]) << 24) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[8]) << 32) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[10]) << 40) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[12]) << 48);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 12:
                        // 00000000_00000000_########_########_########_########_########_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[6]) << 24) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[8]) << 32) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[10]) << 40);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 10:
                        // 00000000_00000000_00000000_########_########_########_########_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[6]) << 24) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[8]) << 32);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 8:
                        // 00000000_00000000_00000000_00000000_########_########_########_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[6]) << 24);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)Math.Min(length, 255) << 56;
                        return code;
                    case 6:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000 | 00000000_00000000_00000000_00000000_00000000_########_00000000_00000000
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[4]) << 16);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 4:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_######## | 00000000_00000000_00000000_00000000_00000000_00000000_########_00000000
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p) | ((ulong)Unsafe.ReadUnaligned<byte>(&p[2]) << 8);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    case 2:
                        // 00000000_00000000_00000000_00000000_00000000_00000000_00000000_########
                        code = (ulong)Unsafe.ReadUnaligned<byte>(p);

                        // ########_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                        code |= (ulong)length << 56;
                        return code;
                    default:
                        code = 0UL;
                        return code;
                }
            }
        }
    }
}
