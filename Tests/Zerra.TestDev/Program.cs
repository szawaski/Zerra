// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Zerra.TestDev
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            var timer = Stopwatch.StartNew();

            //UtfTest();

            //TestMe();
            JsonSerializerTest.TempTestSpeed();
            //ByteSerializerTest.TempTestSpeed();

            //var tester = new EncryptionTest();
            //tester.RandomShiftStreamRead();
            //tester.RandomShiftStreamWrite();
            //tester.RandomShiftEncryption();
            //tester.RandomShiftStreamReadMode();

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

            ////TestCopy.Test();
            ////TestDiscovery.Test();
            ////TestLinqValueExtract.TestGetIDs();
            ////TestLinq.Test();

            //TestRepoSpeed.Test();

            //TestSpeed.Test();
            //TestSpeed.TestAsync().GetAwaiter().GetResult();

            //var length = 1000000;
            //var data = new byte[length];
            //Span<byte> span = stackalloc byte[length];

            //var goob = new GooberClass(length, data, span);

            //goob.GoobInternal();
            //Console.WriteLine();
            //GooberClass.Goob(length, data, span);
            //Console.WriteLine();

            //goob.GoobInternal();
            //Console.WriteLine();
            //GooberClass.Goob(length, data, span);
            //Console.WriteLine();

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
            var iterations = 2000000000;
            var loops = 10;
            var totals = new Dictionary<string, long>();

            totals["Scenario1"] = 0;
            totals["Scenario2"] = 0;

            Holder holder = default;

            for (var j = 0; j < loops; j++)
            {
                Console.Write('.');

                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    Scenario1(ref holder);
                }
                timer.Stop();
                totals["Scenario1"] += timer.ElapsedMilliseconds;

                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    Scenario2(ref holder);
                }
                timer.Stop();
                totals["Scenario2"] += timer.ElapsedMilliseconds;
            }

            Console.WriteLine();
            foreach (var total in totals.OrderBy(x => x.Key))
                Console.WriteLine($"{total.Key} {total.Value / loops}ms");
        }

        private static void Scenario1(ref Holder holder)
        {
            int value;
            DoThing(out value);
            holder.Value = value;
        }

        private static void Scenario2(ref Holder holder)
        {
            DoThing(out holder.Value);
        }

        private struct Holder
        {
            public int Value;
        }
        private static void DoThing(out int value)
        {
            value = 5;
        }
    }
}
