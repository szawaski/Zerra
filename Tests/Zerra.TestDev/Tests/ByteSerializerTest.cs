// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Zerra.Serialization;
using Zerra.Test;

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

        public static Task TempTestSpeed()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };
            var items = Factory.GetCoreTypesModel();
            var data = ByteSerializerOld.Serialize(items, options);
            return TempTestSpeed<CoreTypesModel>(data, options, 20000, 15);
        }

        private static async Task TempTestSpeed<T>(byte[] data, ByteSerializerOptions options, int iterations, int loops)
        {
            var stream = new MemoryStream(data);

            var timer = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++)
            {
                GC.Collect();
                var obj1 = ByteSerializerOld.Deserialize<T>(data, options);
                stream.Position = 0;
                var obj2 = await ByteSerializerOld.DeserializeAsync<T>(stream, options);
                stream.Position = 0;
                var obj3 = await ByteSerializer.DeserializeAsync<T>(stream, options);
                var obj4 = ByteSerializer.Deserialize<T>(data, options);
            }
            Console.WriteLine($"Warmup {timer.ElapsedMilliseconds:n0}ms");

            Console.WriteLine();
            Console.WriteLine($"Serialize");
            Console.WriteLine();

            var totals = new Dictionary<string, long>();
            totals["1 ByteSerializerOld.DeserializeAsync"] = 0;
            totals["2 ByteSerializerOld.Deserialize"] = 0;
            totals["3 ByteSerializer.DeserializeAsync"] = 0;
            totals["4 ByteSerializer.Deserialize"] = 0;

            for (var j = 0; j < loops; j++)
            {
                Console.Write(".");

                //GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    stream.Position = 0;
                    var obj = await ByteSerializerOld.DeserializeAsync<T>(stream, options);
                }
                timer.Stop();
                totals["1 ByteSerializerOld.DeserializeAsync"] += timer.ElapsedMilliseconds;

                //GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    var obj = ByteSerializerOld.Deserialize<T>(data, options);
                }
                timer.Stop();
                totals["2 ByteSerializerOld.Deserialize"] += timer.ElapsedMilliseconds;

                //GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    stream.Position = 0;
                    var obj = await ByteSerializer.DeserializeAsync<T>(stream, options);
                }
                timer.Stop();
                totals["3 ByteSerializer.DeserializeAsync"] += timer.ElapsedMilliseconds;

                //GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    var obj = ByteSerializer.Deserialize<T>(data, options);
                }
                timer.Stop();
                totals["4 ByteSerializer.Deserialize"] += timer.ElapsedMilliseconds;
            }

            Console.WriteLine();
            foreach (var item in totals.OrderBy(x => x.Key))
            {
                Console.WriteLine($"{item.Key} {item.Value / loops}");
            }

            Console.WriteLine();
        }

        public static void TestSpeed()
        {
            Console.WriteLine("Note: Run in Release Mode!");

            var option1 = new ByteSerializerOptions()
            {
                UsePropertyNames = true
            };
            var option2 = new ByteSerializerOptions()
            {
                IncludePropertyTypes = true
            };
            var option3 = new ByteSerializerOptions()
            {
                UsePropertyNames = true,
                IncludePropertyTypes = true
            };

            var items = GetTestStuff();

            var timer0 = Stopwatch.StartNew();
            foreach (var item in items)
            {
                var w1 = ByteSerializer.Serialize(item);
                var w2 = ByteSerializer.Serialize(item, option1);
                var w3 = ByteSerializer.Serialize(item, option2);
                var w4 = ByteSerializer.Serialize(item, option3);
                var w5 = Serialize(item);
                var w6 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));

                var v1 = ByteSerializer.Deserialize<Test>(w1);
                var v2 = ByteSerializer.Deserialize<Test>(w2, option1);
                var v3 = ByteSerializer.Deserialize<Test>(w3, option2);
                var v4 = ByteSerializer.Deserialize<Test>(w4, option3);
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
                var lapDataBytes = ByteSerializer.Serialize(item);
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
                var lapDataBytes = ByteSerializer.Serialize(item, option1);
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
                var lapDataBytes = ByteSerializer.Serialize(item, option2);
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
                var lapDataBytes = ByteSerializer.Serialize(item, option3);
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
                var item = ByteSerializer.Deserialize<Test>(lapDataBytes);
            }
            timer4.Stop();
            Console.WriteLine($"ByteSerializer {timer4.ElapsedMilliseconds:n0}ms");

            //GC.Collect();
            //var timerz = Stopwatch.StartNew();
            //foreach (var lapDataBytes in s1)
            //{
            //    var item = ByteSerializer.NewDeserialize<Test>(lapDataBytes);
            //}
            //timerz.Stop();
            //Console.WriteLine($"ByteSerializerNew {timerz.ElapsedMilliseconds:n0}ms");


            GC.Collect();
            var timer4a = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1a)
            {
                var item = ByteSerializer.Deserialize<Test>(lapDataBytes, option1);
            }
            timer4a.Stop();
            Console.WriteLine($"ByteSerializerNames {timer4a.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer4b = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1b)
            {
                var item = ByteSerializer.Deserialize<Test>(lapDataBytes, option2);
            }
            timer4b.Stop();
            Console.WriteLine($"ByteSerializerTypes {timer4b.ElapsedMilliseconds:n0}ms");

            GC.Collect();
            var timer4c = Stopwatch.StartNew();
            foreach (var lapDataBytes in s1c)
            {
                var item = ByteSerializer.Deserialize<Test>(lapDataBytes, option3);
            }
            timer4c.Stop();
            Console.WriteLine($"ByteSerializerNamesTypes {timer4c.ElapsedMilliseconds:n0}ms");

            //GC.Collect();
            //var timerz4 = Stopwatch.StartNew();
            //foreach (var lapDataBytes in s1c)
            //{
            //    var item = ByteSerializer.NewDeserialize<Test>(lapDataBytes);
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
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var binaryFormatter = new BinaryFormatter();
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
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var binaryFormatter = new BinaryFormatter();
            var obj = binaryFormatter.Deserialize(memoryStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            memoryStream.Close();
            memoryStream.Dispose();
            return obj;
        }
    }
}
