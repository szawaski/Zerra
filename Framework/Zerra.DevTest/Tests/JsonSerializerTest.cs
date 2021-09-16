// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.Text;
using Zerra.IO;
using Zerra.Serialization;

namespace Zerra.DevTest
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

        public static void TestSpeed()
        {
            const int testlength = 30000;
            const int warmupLength = 100;

            var timer0 = Stopwatch.StartNew();
            for (var i = 0; i < warmupLength; i++)
            {
                Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
                Newtonsoft.Json.JsonConvert.DeserializeObject<TestObject>(json, newtonsoftConverter);

                Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
                Utf8Json.JsonSerializer.Deserialize<TestObject>(Encoding.UTF8.GetBytes(json));

                System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
                System.Text.Json.JsonSerializer.Deserialize<TestObject>(json, systemTextJsonOptions);

                JsonSerializer.Serialize(obj);
                JsonSerializer.Deserialize<TestObject>(json);
            }
            timer0.Start();
            Console.WriteLine("{0} Warmup", timer0.ElapsedMilliseconds);


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
                Console.WriteLine("{0}b JsonSerializer", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = JsonSerializer.SerializeNameless(obj);
                Console.WriteLine("{0}b JsonSerializerNameless", Encoding.UTF8.GetBytes(result).Length);
            }


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Serialize");

            GC.Collect();
            var timer1 = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
            }
            timer1.Stop();
            Console.WriteLine("{0} JsonConvert", timer1.ElapsedMilliseconds);

            GC.Collect();
            var timer1a = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
            }
            timer1a.Stop();
            Console.WriteLine("{0} Utf8Json", timer1a.ElapsedMilliseconds);

            var timer1b = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(obj);
            }
            timer1b.Stop();
            Console.WriteLine("{0} System.Text.Json", timer1b.ElapsedMilliseconds);

            GC.Collect();
            var timer2d = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.Serialize(obj);
            }
            timer2d.Stop();
            Console.WriteLine("{0} JsonSerializer {1}", timer2d.ElapsedMilliseconds, Math.Round((timer1.ElapsedMilliseconds / (decimal)timer2d.ElapsedMilliseconds) * 100, 1));

            GC.Collect();
            var timer2e = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.SerializeNameless(obj);
            }
            timer2e.Stop();
            Console.WriteLine("{0} JsonSerializer.ToJsonNameless {1}", timer2e.ElapsedMilliseconds, Math.Round((timer1.ElapsedMilliseconds / (decimal)timer2e.ElapsedMilliseconds) * 100, 1));


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Deserialize");

            GC.Collect();
            var timer3 = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TestObject>(json);
            }
            timer3.Stop();
            Console.WriteLine("{0} JsonConvert", timer3.ElapsedMilliseconds);

            GC.Collect();
            var timer3a = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Utf8Json.JsonSerializer.Deserialize<TestObject>(Encoding.UTF8.GetBytes(json));
            }
            timer3a.Stop();
            Console.WriteLine("{0} Utf8Json", timer3a.ElapsedMilliseconds);

            GC.Collect();
            var timer3b = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<TestObject>(json);
            }
            timer3b.Stop();
            Console.WriteLine("{0} System.Text.Json", timer3b.ElapsedMilliseconds);

            GC.Collect();
            var timer4 = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.Deserialize<TestObject>(json);
            }
            timer4.Stop();
            Console.WriteLine("{0} JsonSerializer", timer4.ElapsedMilliseconds);

            var timer5 = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializer.DeserializeNameless<TestObject>(jsonnameless);
            }
            timer5.Stop();
            Console.WriteLine("{0} JsonSerializer.FromJsonNameless", timer5.ElapsedMilliseconds);

            Console.WriteLine();
        }

        [Benchmark]
        public void SerializeZerra()
        {
            JsonSerializer.Serialize(obj);
        }

        [Benchmark]
        public void SerializeZerraNameless()
        {
            JsonSerializer.SerializeNameless(obj);
        }

        [Benchmark]
        public void SerializeNewtonsoft()
        {
            Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        [Benchmark]
        public void SerializeSystemTextJson()
        {
            System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
        }

        [Benchmark]
        public void SerializeUtf8Json()
        {
            Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
        }

        public class TestObject2
        {
            public bool Pbool { get; set; }
            public byte Pbyte { get; set; }
            public sbyte Psbyte { get; set; }
            public ushort Pushort { get; set; }
        }
    }
}