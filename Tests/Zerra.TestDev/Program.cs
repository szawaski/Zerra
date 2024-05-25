// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using System.Diagnostics;
using System.IO;
using Zerra.Serialization;

namespace Zerra.TestDev
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            var timer = Stopwatch.StartNew();

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
    }
}
