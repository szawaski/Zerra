// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Zerra.Serialization.Json;
using Zerra.Test;

namespace Zerra.TestDev
{
    [MemoryDiagnoser]
    public class JsonSerializerTest
    {
        private static readonly System.Text.Json.JsonSerializerOptions systemTextJsonOptions;
        private static readonly Newtonsoft.Json.Converters.StringEnumConverter newtonsoftConverter;
        private static readonly AllTypesModel obj;
        private static readonly string json;
        private static readonly string jsonnameless;

        private static readonly JsonSerializerOptionsOld optionsNameless = new()
        {
            Nameless = true
        };

        static JsonSerializerTest()
        {
            systemTextJsonOptions = new System.Text.Json.JsonSerializerOptions();
            systemTextJsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            newtonsoftConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
            obj = AllTypesModel.Create();
            json = JsonSerializerOld.Serialize(obj);
            jsonnameless = JsonSerializerOld.Serialize(obj, optionsNameless);
        }

        public static async Task TestSpeed()
        {
  
            Console.WriteLine("Note: Run in Release Mode!");

#if DEBUG
            const int testlength = 100;
#else
            const int testlength = 10000;
#endif
            const int warmupLength = 100;

            var timer = Stopwatch.StartNew();
            for (var i = 0; i < warmupLength; i++)
            {
                _ = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
                _ = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json, newtonsoftConverter);

                _ = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
                _ = Utf8Json.JsonSerializer.Deserialize<AllTypesModel>(Encoding.UTF8.GetBytes(json));

                _ = System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
                _ = System.Text.Json.JsonSerializer.Deserialize<AllTypesModel>(json, systemTextJsonOptions);

                _ = JsonSerializerOld.Serialize(obj);
                _ = JsonSerializerOld.Deserialize<AllTypesModel>(json);
            }
            timer.Start();
            Console.WriteLine("{0} Warmup", timer.ElapsedMilliseconds);


            Console.WriteLine();
            //------------------------------------------------------------------------------------------------------------------------------------------------------------

            Console.WriteLine("Sizes");
            {
                var result = Newtonsoft.Json.JsonConvert.SerializeObject(obj, newtonsoftConverter);
                Console.WriteLine("{0}b Newtonsoft.Json", Encoding.UTF8.GetBytes(result).Length);
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
                var result = JsonSerializerOld.Serialize(obj);
                Console.WriteLine("{0}b Zerra.Serialization", Encoding.UTF8.GetBytes(result).Length);
            }
            {
                var result = JsonSerializerOld.Serialize(obj, optionsNameless);
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
            Console.WriteLine("{0} Newtonsoft.Json", timer.ElapsedMilliseconds);

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
                var result = System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
            }
            timer.Stop();
            Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Serialize(obj);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Serialize(obj, optionsNameless);
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
                    await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, systemTextJsonOptions);
                }
                timer.Stop();
                Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await JsonSerializerOld.SerializeAsync(stream, obj);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    await JsonSerializerOld.SerializeAsync(stream, obj, optionsNameless);
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
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} JsonConvert", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Utf8Json.JsonSerializer.Deserialize<AllTypesModel>(Encoding.UTF8.GetBytes(json));
            }
            timer.Stop();
            Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<AllTypesModel>(json, systemTextJsonOptions);
            }
            timer.Stop();
            Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Deserialize<AllTypesModel>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Deserialize<AllTypesModel>(jsonnameless, optionsNameless);
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
                    var result = await Utf8Json.JsonSerializer.DeserializeAsync<AllTypesModel>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await System.Text.Json.JsonSerializer.DeserializeAsync<AllTypesModel>(stream, systemTextJsonOptions);
                }
                timer.Stop();
                Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    streamNameless.Position = 0;
                    var result = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(streamNameless, optionsNameless);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization-Nameless", timer.ElapsedMilliseconds);
            }

            Console.WriteLine();
        }

        [Benchmark]
        public void SerializeZerra()
        {
            _ = JsonSerializerOld.Serialize(obj);
        }

        [Benchmark]
        public void SerializeZerraNameless()
        {
            _ = JsonSerializerOld.Serialize(obj, optionsNameless);
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
    }
}