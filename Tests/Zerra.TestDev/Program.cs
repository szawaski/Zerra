// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Zerra.Serialization;

namespace Zerra.TestDev
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            var timer = Stopwatch.StartNew();

            //TestMe();
            ByteSerializerTest.TempTestSpeed();

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
