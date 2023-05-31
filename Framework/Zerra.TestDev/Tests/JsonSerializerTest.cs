// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.TestDev
{
    [MemoryDiagnoser]
    public class JsonSerializerTest
    {
        private static readonly System.Text.Json.JsonSerializerOptions systemTextJsonOptions;
        private static readonly Newtonsoft.Json.Converters.StringEnumConverter newtonsoftConverter;
        private static readonly TestObject obj;
        private static readonly string json;
        private static readonly string jsonnameless;
        static JsonSerializerTest()
        {
            systemTextJsonOptions = new System.Text.Json.JsonSerializerOptions();
            systemTextJsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            newtonsoftConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
            obj = GetTestObject();
            json = JsonSerializer.Serialize(obj);
            jsonnameless = JsonSerializer.SerializeNameless(obj);
        }

        public static async Task TestSpeed()
        {
            Console.WriteLine("Note: Run in Release Mode!");

            const int testlength = 30000;
            const int warmupLength = 100;

            var timer = Stopwatch.StartNew();
            for (var i = 0; i < warmupLength; i++)
            {
                _ = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
                _ = Newtonsoft.Json.JsonConvert.DeserializeObject<TestObject>(json, newtonsoftConverter);

                _ = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
                _ = Utf8Json.JsonSerializer.Deserialize<TestObject>(Encoding.UTF8.GetBytes(json));

                _ = System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
                _ = System.Text.Json.JsonSerializer.Deserialize<TestObject>(json, systemTextJsonOptions);

                _ = JsonSerializer.Serialize(obj);
                _ = JsonSerializer.Deserialize<TestObject>(json);
            }
            timer.Start();
            Console.WriteLine("{0} Warmup", timer.ElapsedMilliseconds);


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Sizes");
            {
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
                Console.WriteLine("{0}b JsonConvert", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
                Console.WriteLine("{0}b Utf8Json", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = System.Text.Json.JsonSerializer.Serialize(obj);
                Console.WriteLine("{0}b System.Text.Json", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = JsonSerializer.Serialize(obj);
                Console.WriteLine("{0}b Zerra.Serialization", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = JsonSerializer.SerializeNameless(obj);
                Console.WriteLine("{0}b Zerra.Serialization-Nameless", Encoding.UTF8.GetBytes(result).Length);
            }


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Serialize");

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
            }
            timer.Stop();
            Console.WriteLine("{0} JsonConvert", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
            }
            timer.Stop();
            Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(obj);
            }
            timer.Stop();
            Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.Serialize(obj);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.SerializeNameless(obj);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization-Nameless", timer.ElapsedMilliseconds);


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("SerializeAsync");
            using (var stream = new MemoryStream())
            {
                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await Utf8Json.JsonSerializer.SerializeAsync(stream, obj);
                }
                timer.Stop();
                Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj);
                }
                timer.Stop();
                Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await JsonSerializer.SerializeAsync(stream, obj);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await JsonSerializer.SerializeNamelessAsync(stream, obj);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization-Nameless", timer.ElapsedMilliseconds);
            }

            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Deserialize");

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TestObject>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} JsonConvert", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Utf8Json.JsonSerializer.Deserialize<TestObject>(Encoding.UTF8.GetBytes(json));
            }
            timer.Stop();
            Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<TestObject>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.Deserialize<TestObject>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.DeserializeNameless<TestObject>(jsonnameless);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization-Nameless", timer.ElapsedMilliseconds);

            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("DeserializeAsync");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            using (var streamNameless = new MemoryStream(Encoding.UTF8.GetBytes(jsonnameless)))
            {
                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await Utf8Json.JsonSerializer.DeserializeAsync<TestObject>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await System.Text.Json.JsonSerializer.DeserializeAsync<TestObject>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await JsonSerializer.DeserializeAsync<TestObject>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    streamNameless.Position = 0;
                    var result = await JsonSerializer.DeserializeNamelessAsync<TestObject>(streamNameless);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization-Nameless", timer.ElapsedMilliseconds);
            }

            Console.WriteLine();
        }

        [Benchmark]
        public void SerializeZerra()
        {
            _ = JsonSerializer.Serialize(obj);
        }

        [Benchmark]
        public void SerializeZerraNameless()
        {
            _ = JsonSerializer.SerializeNameless(obj);
        }

        [Benchmark]
        public void SerializeNewtonsoft()
        {
            _ = Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        [Benchmark]
        public void SerializeSystemTextJson()
        {
            _ = System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
        }

        [Benchmark]
        public void SerializeUtf8Json()
        {
            _ = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
        }

        public class TestObject
        {
            public bool Pbool { get; set; }
            public byte Pbyte { get; set; }
            public sbyte Psbyte { get; set; }
            public ushort Pushort { get; set; }
            public short Pshort { get; set; }
            public uint Puint { get; set; }
            public int Pint { get; set; }
            public ulong Pulong { get; set; }
            public long Plong { get; set; }
            public float Pfloat { get; set; }
            public double Pdouble { get; set; }
            public decimal Pdecimal { get; set; }
            public char Pchar { get; set; }
            public string Pstring { get; set; }
            public DateTime Pdatetime { get; set; }
            public DateTimeOffset Pdatetimeoffset { get; set; }
            public Guid Pguid { get; set; }

            public bool[] Abool { get; set; }
            public decimal[] Adecimal { get; set; }
            public string[] Astring { get; set; }

            public TestObject Pobject { get; set; }

            public TestObject[] AObject { get; set; }
        }

        public class TestObject2
        {
            public bool Pbool { get; set; }
            public byte Pbyte { get; set; }
            public sbyte Psbyte { get; set; }
            public ushort Pushort { get; set; }
        }

        private static TestObject GetTestObject()
        {
            return new TestObject()
            {
                Pbool = true,
                Pbyte = 1,
                Psbyte = -2,
                Pushort = 3,
                Pshort = -4,
                Puint = 5,
                Pint = -6,
                Pulong = 7,
                Plong = -8,
                Pfloat = 9.1f,
                Pdouble = 10.2,
                Pdecimal = 11.3m,
                Pchar = 'A',
                //Pstring = "http://\twww.hi.com",
                Pstring = "asdfasdfasdf",
                Pdatetime = DateTime.Now,
                Pdatetimeoffset = DateTimeOffset.Now,
                Pguid = Guid.NewGuid(),

                Abool = new bool[] { true, false, true },
                Adecimal = new decimal[] { 1.1m, 2.2m, 3.3m },
                Astring = new string[] { "aaa", "bbb", "ccc" },

                Pobject = new TestObject() { Pstring = "Stuff" },
                AObject = new TestObject[] { new TestObject() { Pstring = "Thing1" }, new TestObject() { Pstring = "Thing2" } }
            };
        }
    }
}