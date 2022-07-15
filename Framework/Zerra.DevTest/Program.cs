// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using System.Buffers;
using System.Diagnostics;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.DevTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var loops = 10000;
            //var tasks = new List<Task>();
            //for (var i = 0; i < loops; i++)
            //    tasks.Add(Task.Run(PointerTest));

            //GC.Collect();
            //Task.WaitAll(tasks.ToArray());

            var timer1 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
                PointerTest2();
            timer1.Stop();
            Console.WriteLine(timer1.ElapsedMilliseconds);

            var timer2 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
                PointerTest();
            timer2.Stop();
            Console.WriteLine(timer2.ElapsedMilliseconds);

            var timer3 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
                PointerTest2();
            timer3.Stop();
            Console.WriteLine(timer3.ElapsedMilliseconds);

            var timer4 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
                PointerTest();
            timer4.Stop();
            Console.WriteLine(timer4.ElapsedMilliseconds);

            //var tester = new EncryptionTest();
            //tester.RandomShiftStreamRead();
            //tester.RandomShiftStreamWrite();
            //tester.RandomShiftEncryption();
            //tester.RandomShiftStreamReadMode();

            //TestMemory.TryFinallyOrDisposed();
            //InlineTest.Test();

            //JsonSerializerTest.TestSpeed();
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

            Console.WriteLine("Done");
            _ = Console.ReadLine();
        }

        public static unsafe void PointerTest()
        {
            var stuff = ArrayPool<int>.Shared.Rent(524288);

            for (var i = 0; i < stuff.Length; i++)
                stuff[i] = i;

            fixed (int* pstuffFixed = stuff)
            {
                var pstuff = pstuffFixed;
                for (var i = 0; i < stuff.Length; i++)
                {
                    *pstuff = *pstuff + 1;
                    pstuff++;
                }
            }

            ArrayPool<int>.Shared.Return(stuff);
        }

        public static unsafe void PointerTest2()
        {
            var stuff = ArrayPool<int>.Shared.Rent(524288);

            for (var i = 0; i < stuff.Length; i++)
                stuff[i] = i;

            fixed (int* pstuffFixed = stuff)
            {
                for (var i = 0; i < stuff.Length; i++)
                {
                    stuff[i] = stuff[i] + 1;
                }
            }

            ArrayPool<int>.Shared.Return(stuff);
        }

        public ref struct GooberClass
        {
            private int length;
            private byte[] data;
            private Span<byte> span;

            public GooberClass(int length, byte[] data, Span<byte> span)
            {
                this.length = length;
                this.data = data;
                this.span = span;
            }

            public void GoobInternal()
            {
                var t1 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    data[i] = 1;
                }
                t1.Stop();
                Console.WriteLine($"Array: {t1.ElapsedMilliseconds}");

                var t2 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    span[i] = 2;
                }
                t2.Stop();
                Console.WriteLine($"Span: {t2.ElapsedMilliseconds}");


                t1 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    data[i] = 1;
                }
                t1.Stop();
                Console.WriteLine($"Array: {t1.ElapsedMilliseconds}");

                t2 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    span[i] = 2;
                }
                t2.Stop();
                Console.WriteLine($"Span: {t2.ElapsedMilliseconds}");
            }

            public static void Goob(int length, byte[] data, Span<byte> span)
            {
                var t1 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    data[i] = 1;
                }
                t1.Stop();
                Console.WriteLine($"Array: {t1.ElapsedMilliseconds}");

                var t2 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    span[i] = 2;
                }
                t2.Stop();
                Console.WriteLine($"Span: {t2.ElapsedMilliseconds}");


                t1 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    data[i] = 1;
                }
                t1.Stop();
                Console.WriteLine($"Array: {t1.ElapsedMilliseconds}");

                t2 = Stopwatch.StartNew();
                for (var i = 0; i < length; i++)
                {
                    span[i] = 2;
                }
                t2.Stop();
                Console.WriteLine($"Span: {t2.ElapsedMilliseconds}");
            }
        }
    }

    public class thing
    {
        public Guid ID { get; set; }
    }

    public class MSILCHECK : ICoreTypeSetter<thing>
    {
        public CoreType? CoreType => Reflection.CoreType.Guid;

        public bool IsByteArray => false;

        public void Setter(thing model, bool value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, sbyte value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, short value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ushort value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, int value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, uint value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, long value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ulong value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, float value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, double value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, decimal value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, char value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTime value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTimeOffset value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeSpan value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, Guid value)
        {
            model.ID = value;
        }

        public void Setter(thing model, string value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, bool? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, sbyte? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, short? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ushort? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, int? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, uint? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, long? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, ulong? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, float? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, double? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, decimal? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, char? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTime? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, DateTimeOffset? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, TimeSpan? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, Guid? value)
        {
            throw new NotImplementedException();
        }

        public void Setter(thing model, byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
