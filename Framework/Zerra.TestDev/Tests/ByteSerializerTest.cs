// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Zerra.Serialization;

namespace Zerra.TestDev
{
    public static class ByteSerializerTest
    {
        [Serializable]
        private sealed class Test
        {
            public string Name { get; set; }
            public int[] Things { get; set; }
            public Test2[] Test2 { get; set; }
        }
        [Serializable]
        private sealed class Test2
        {
            public int? Thing { get; set; }
        }

        private static IList<Test> GetTestStuff()
        {
            var thingsList = new List<int>();
            for (var i = 0; i < 10000; i++)
            {
                thingsList.Add(i);
            }
            var testList = new List<Test>();
            for (var i = 0; i < 1000; i++)
            {
                var test2List = new List<Test2>();
                for (var j = 0; j < 1000; j++)
                {
                    var test2 = new Test2()
                    {
                        Thing = j
                    };
                    test2List.Add(test2);
                }
                var item = new Test()
                {
                    Name = Guid.NewGuid().ToString(),
                    Things = thingsList.ToArray(),
                    Test2 = test2List.ToArray()
                };
                testList.Add(item);
            }
            return testList;
        }

        public static void TestSpeed()
        {
            Console.WriteLine("Note: Run in Release Mode!");

            var items = GetTestStuff();

            var timer0 = Stopwatch.StartNew();
            foreach (var item in items)
            {
                var w1 = new ByteSerializer().Serialize(item);
                var w2 = new ByteSerializer(true, false).Serialize(item);
                var w3 = new ByteSerializer(false, true).Serialize(item);
                var w4 = new ByteSerializer(true, true).Serialize(item);
                var w5 = Serialize(item);
                var w6 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));

                var v1 = new ByteSerializer().Deserialize<Test>(w1);
                var v2 = new ByteSerializer(true, false).Deserialize<Test>(w2);
                var v3 = new ByteSerializer(false, true).Deserialize<Test>(w3);
                var v4 = new ByteSerializer(true, true).Deserialize<Test>(w4);
                var v5 = Deserialize(w5);
                var v6 = JsonConvert.DeserializeObject<Test>(Encoding.UTF8.GetString(w6));
            }
            Console.WriteLine($"Warmup {timer0.ElapsedMilliseconds:n0}ms");

            Console.WriteLine();
            Console.WriteLine($"Serialize");

            GC.Collect();
            var timer1 = Stopwatch.StartNew();
            var s1 = new List<byte[]>();
            long totalSize1 = 0;
            foreach (var item in items)
            {
                var s = new ByteSerializer();
                var lapDataBytes = s.Serialize(item);
                totalSize1 += lapDataBytes.Length;
                s1.Add(lapDataBytes);
            }
            timer1.Stop();
            Console.WriteLine($"ByteSerializer {timer1.ElapsedMilliseconds:n0}ms {totalSize1 / 1000:n0}kb");

            GC.Collect();
            var timer1a = Stopwatch.StartNew();
            var s1a = new List<byte[]>();
            long totalSize1a = 0;
            foreach (var item in items)
            {
                var s = new ByteSerializer(true, false);
                var lapDataBytes = s.Serialize(item);
                totalSize1a += lapDataBytes.Length;
                s1a.Add(lapDataBytes);
            }
            timer1a.Stop();
            Console.WriteLine($"ByteSerializerNames {timer1a.ElapsedMilliseconds:n0}ms {totalSize1a / 1000:n0}kb");

            GC.Collect();
            var timer1b = Stopwatch.StartNew();
            var s1b = new List<byte[]>();
            long totalSize1b = 0;
            foreach (var item in items)
            {
                var s = new ByteSerializer(false, true);
                var lapDataBytes = s.Serialize(item);
                totalSize1b += lapDataBytes.Length;
                s1b.Add(lapDataBytes);
            }
            timer1b.Stop();
            Console.WriteLine($"ByteSerializerTypes {timer1b.ElapsedMilliseconds:n0}ms {totalSize1b / 1000:n0}kb");

            GC.Collect();
            var timer1c = Stopwatch.StartNew();
            var s1c = new List<byte[]>();
            long totalSize1c = 0;
            foreach (var item in items)
            {
                var s = new ByteSerializer(true, true);
                var lapDataBytes = s.Serialize(item);
                totalSize1c += lapDataBytes.Length;
                s1c.Add(lapDataBytes);
            }
            timer1c.Stop();
            Console.WriteLine($"ByteSerializerNamesTypes {timer1c.ElapsedMilliseconds:n0}ms {totalSize1c / 1000:n0}kb");

            GC.Collect();
            var timer2 = Stopwatch.StartNew();
            var s2 = new List<byte[]>();
            long totalSize2 = 0;
            foreach (var item in items)
            {
                var lapDataBytes = Serialize(item);
                totalSize2 += lapDataBytes.Length;
                s2.Add(lapDataBytes);
            }
            timer2.Stop();
            Console.WriteLine($"BinaryFormatter {timer2.ElapsedMilliseconds:n0}ms {totalSize2 / 1000:n0}kb");

            GC.Collect();
            var timer3 = Stopwatch.StartNew();
            var s3 = new List<byte[]>();
            long totalSize3 = 0;
            foreach (var item in items)
            {
                var lapDataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                totalSize3 += lapDataBytes.Length;
                s3.Add(lapDataBytes);
            }
            timer3.Stop();
            Console.WriteLine($"JsonConvert {timer3.ElapsedMilliseconds:n0}ms {totalSize3 / 1000:n0}kb");


            Console.WriteLine();
            Console.WriteLine($"Deserialize");

            GC.Collect();
            var timer4 = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1)
            {
                var s = new ByteSerializer();
                var item = s.Deserialize<Test>(lapDataBytes);
            }
            timer4.Stop();
            Console.WriteLine($"ByteSerializer {timer4.ElapsedMilliseconds:n0}ms");

            //GC.Collect();
            //var timerz = Stopwatch.StartNew();
            //foreach (var lapDataBytes in s1)
            //{
            //    var s = new ByteSerializer();
            //    var item = s.NewDeserialize<Test>(lapDataBytes);
            //}
            //timerz.Stop();
            //Console.WriteLine($"ByteSerializerNew {timerz.ElapsedMilliseconds:n0}ms");


            GC.Collect();
            var timer4a = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1a)
            {
                var s = new ByteSerializer(true, false);
                var item = s.Deserialize<Test>(lapDataBytes);
            }
            timer4a.Stop();
            Console.WriteLine($"ByteSerializerNames {timer4a.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer4b = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1b)
            {
                var s = new ByteSerializer(false, true);
                var item = s.Deserialize<Test>(lapDataBytes);
            }
            timer4b.Stop();
            Console.WriteLine($"ByteSerializerTypes {timer4b.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer4c = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1c)
            {
                var s = new ByteSerializer(true, true);
                var item = s.Deserialize<Test>(lapDataBytes);
            }
            timer4c.Stop();
            Console.WriteLine($"ByteSerializerNamesTypes {timer4c.ElapsedMilliseconds:n0}ms");

            //GC.Collect();
            //var timerz4 = Stopwatch.StartNew();
            //foreach (var lapDataBytes in s1c)
            //{
            //    var s = new ByteSerializer(true, true);
            //    var item = s.NewDeserialize<Test>(lapDataBytes);
            //}
            //timerz4.Stop();
            //Console.WriteLine($"ByteSerializerNamesTypesNew {timerz4.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer5 = Stopwatch.StartNew();
            foreach (var lapDataBytes in s2)
            {
                var item = Deserialize(lapDataBytes);
            }
            timer5.Stop();
            Console.WriteLine($"BinaryFormatter {timer5.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer6 = Stopwatch.StartNew();
            foreach (var lapDataBytes in s3)
            {
                var item = JsonConvert.DeserializeObject<Test>(Encoding.UTF8.GetString(lapDataBytes));
            }
            timer6.Stop();
            Console.WriteLine($"JsonConvert {timer6.ElapsedMilliseconds:n0}");

            Console.WriteLine();
        }


        private static byte[] Serialize(object obj)
        {
            if (obj == null)
                return null;
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            binaryFormatter.Serialize(memoryStream, obj);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            memoryStream.Flush();
            _ = memoryStream.Seek(0, SeekOrigin.Begin);
            var bytes = memoryStream.GetBuffer();
            memoryStream.Close();
            memoryStream.Dispose();
            return bytes;
        }

        private static object Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            var memoryStream = new MemoryStream();
            memoryStream.Write(bytes, 0, bytes.Length);
            _ = memoryStream.Seek(0, SeekOrigin.Begin);
            var binaryFormatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var obj = binaryFormatter.Deserialize(memoryStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            memoryStream.Close();
            memoryStream.Dispose();
            return obj;
        }
    }
}
