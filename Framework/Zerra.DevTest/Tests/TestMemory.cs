// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;

namespace Zerra.DevTest
{
    [MemoryDiagnoser]
    public class TestMemory
    {
        public static void Test()
        {
            const int loops = 100000000;
            const long value = Int64.MaxValue;

            var bufferW = new byte[sizeof(long) * loops];
            var timerW = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
            {
                var bytes = BitConverter.GetBytes(value);
                Buffer.BlockCopy(bytes, 0, bufferW, i * sizeof(long), sizeof(long));
            }
            timerW.Stop();
            Console.WriteLine($"Warmup {timerW.ElapsedMilliseconds}");

            var buffer1 = new byte[sizeof(long) * loops];
            var timer1 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
            {
                Unsafe.As<byte, long>(ref buffer1[i * sizeof(long)]) = value;
            }
            timer1.Stop();
            Console.WriteLine($"Unsafe.As {timer1.ElapsedMilliseconds}");

            var buffer2 = new byte[sizeof(long) * loops];
            var timer2 = Stopwatch.StartNew();
            unchecked
            {
                for (var i = 0; i < loops; i++)
                {
                    buffer2[i + 0] = (byte)value;
                    buffer2[i + 1] = (byte)(value >> 8);
                    buffer2[i + 2] = (byte)(value >> 16);
                    buffer2[i + 3] = (byte)(value >> 24);
                    buffer2[i + 4] = (byte)(value >> 32);
                    buffer2[i + 5] = (byte)(value >> 40);
                    buffer2[i + 6] = (byte)(value >> 48);
                    buffer2[i + 7] = (byte)(value >> 56);
                }
            }
            timer2.Stop();
            Console.WriteLine($"BitShift {timer2.ElapsedMilliseconds}");

            var buffer4 = new byte[sizeof(long) * loops];
            var timer4 = Stopwatch.StartNew();
            for (var i = 4; i < loops; i++)
            {
                var bytes = BitConverter.GetBytes(value);
                buffer4[i + 0] = bytes[0];
                buffer4[i + 1] = bytes[1];
                buffer4[i + 2] = bytes[2];
                buffer4[i + 3] = bytes[3];
                buffer4[i + 4] = bytes[4];
                buffer4[i + 5] = bytes[5];
                buffer4[i + 6] = bytes[6];
                buffer4[i + 7] = bytes[7];
            }
            timer4.Stop();
            Console.WriteLine($"BitConverter.GetBytes+Assign {timer4.ElapsedMilliseconds}");

            var buffer3 = new byte[sizeof(long) * loops];
            var timer3 = Stopwatch.StartNew();
            for (var i = 3; i < loops; i++)
            {
                var bytes = BitConverter.GetBytes(value);
                Buffer.BlockCopy(bytes, 0, buffer3, i * sizeof(long), sizeof(long));
            }
            timer3.Stop();
            Console.WriteLine($"BitConverter.GetBytes+Buffer.BlockCopy {timer3.ElapsedMilliseconds}");


        }

        public static void CharWriteBufferSpeed()
        {
            const int testlength = 30000;
            const int sbLoops = 100;
            var sbString = "asdfasdfasdfasdfasdfasdfa15245234523452asfasfasdfzsdfsdfs";

            var timerZa = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var sb = new StringBuilder();
                for (var j = 0; j < sbLoops; j++)
                {
                    _ = sb.Append(sbString);
                    _ = sb.Append(sbString);
                    _ = sb.Append(j);
                }
                var result = sb.ToString();
            }
            timerZa.Stop();
            Console.WriteLine("{0} StringBuilder", timerZa.ElapsedMilliseconds);

            var timerZd = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var sb = new CharWriter();
                try
                {
                    for (var j = 0; j < sbLoops; j++)
                    {
                        sb.Write(sbString);
                        sb.Write(sbString);
                        sb.Write(j);
                    }
                    var result = sb.ToString();
                }
                finally
                {
                    sb.Dispose();
                }
            }
            timerZd.Stop();
            Console.WriteLine("{0} CharWriteBuffer", timerZd.ElapsedMilliseconds);

            Console.WriteLine();
        }
    }
}
