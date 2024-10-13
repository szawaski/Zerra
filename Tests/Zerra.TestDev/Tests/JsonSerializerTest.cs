// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static readonly TypesAllModel obj;
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
            obj = TypesAllModel.Create();
            json = JsonSerializerOld.Serialize(obj);
            jsonnameless = JsonSerializerOld.Serialize(obj, optionsNameless);
        }

        public static Task TempTestSpeed()
        {
            var item = TypesAllModel.Create();
            var data = System.Text.Json.JsonSerializer.Serialize(item);

            var method = typeof(JsonSerializerTest).GetMethod(nameof(TempTestSpeed2), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(item.GetType());
            return (Task)method.Invoke(null, [item, data, 5000, 5]);
        }

        private static async Task TempTestSpeed2<T>(T item, string data, int iterations, int loops)
        {
            using var readStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var writeStream = new MemoryStream();

            Console.WriteLine($"Warmup");

            var timer = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++)
            {
                readStream.Position = 0;
                _ = await JsonSerializerOld.DeserializeAsync<T>(readStream);
                readStream.Position = 0;
                _ = await JsonSerializer.DeserializeAsync<T>(readStream);
                _ = JsonSerializerOld.Deserialize<T>(data);
                _ = JsonSerializer.Deserialize<T>(data);
                _ = System.Text.Json.JsonSerializer.Deserialize<T>(data);

                writeStream.Position = 0;
                await JsonSerializerOld.SerializeAsync(writeStream, item);
                writeStream.Position = 0;
                await JsonSerializer.SerializeAsync(writeStream, item);
                _ = JsonSerializerOld.Serialize(item);
                _ = JsonSerializer.Serialize(item);
                _ = System.Text.Json.JsonSerializer.Serialize(item);

                GC.Collect();
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Done {timer.ElapsedMilliseconds:n0}ms");

            Console.WriteLine();
            Console.WriteLine($"Running");

            var totalsOld = new Dictionary<string, long>();
            var totals = new Dictionary<string, long>();
            var totalsMs = new Dictionary<string, long>();

            totalsOld["1 DeserializeAsync"] = 0;
            totalsOld["2 Deserialize"] = 0;
            totalsOld["3 SerializeAsync"] = 0;
            totalsOld["4 Serialize"] = 0;

            totals["1 DeserializeAsync"] = 0;
            totals["2 Deserialize"] = 0;
            totals["3 SerializeAsync"] = 0;
            totals["4 Serialize"] = 0;

            totalsMs["1 DeserializeAsync"] = 0;
            totalsMs["2 Deserialize"] = 0;
            totalsMs["3 SerializeAsync"] = 0;
            totalsMs["4 Serialize"] = 0;

            for (var j = 0; j < loops; j++)
            {
                Console.Write(".");

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    readStream.Position = 0;
                    _ = await JsonSerializerOld.DeserializeAsync<T>(readStream);
                }
                timer.Stop();
                totalsOld["1 DeserializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    readStream.Position = 0;
                    _ = await JsonSerializer.DeserializeAsync<T>(readStream);
                }
                timer.Stop();
                totals["1 DeserializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    readStream.Position = 0;
                    _ = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(readStream);
                }
                timer.Stop();
                totalsMs["1 DeserializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = JsonSerializerOld.Deserialize<T>(data);
                }
                timer.Stop();
                totalsOld["2 Deserialize"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = JsonSerializer.Deserialize<T>(data);
                }
                timer.Stop();
                totals["2 Deserialize"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = System.Text.Json.JsonSerializer.Deserialize<T>(data);
                }
                timer.Stop();
                totalsMs["2 Deserialize"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    writeStream.Position = 0;
                    await JsonSerializerOld.SerializeAsync(writeStream, item);
                }
                timer.Stop();
                totalsOld["3 SerializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    writeStream.Position = 0;
                    await JsonSerializer.SerializeAsync(writeStream, item);
                }
                timer.Stop();
                totals["3 SerializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    writeStream.Position = 0;
                    await System.Text.Json.JsonSerializer.SerializeAsync(writeStream, item);
                }
                timer.Stop();
                totalsMs["3 SerializeAsync"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = JsonSerializerOld.Serialize(item);
                }
                timer.Stop();
                totalsOld["4 Serialize"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = JsonSerializer.Serialize(item);
                }
                timer.Stop();
                totals["4 Serialize"] += timer.ElapsedMilliseconds;

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < iterations; i++)
                {
                    _ = System.Text.Json.JsonSerializer.Serialize(item);
                }
                timer.Stop();
                totalsMs["4 Serialize"] += timer.ElapsedMilliseconds;
            }

            Console.WriteLine();
            foreach (var total in totals.OrderBy(x => x.Key))
            {
                var totalOld = totalsOld[total.Key];
                Console.WriteLine($"{total.Key} {total.Value / loops}/{totalOld / loops} {Math.Round((((decimal)total.Value / loops) / ((decimal)totalOld / loops)) * 100, 2)}%");
            }

            Console.WriteLine();
            foreach (var total in totals.OrderBy(x => x.Key))
            {
                var totalMs = totalsMs[total.Key];
                Console.WriteLine($"{total.Key} {total.Value / loops}/{totalMs / loops} {Math.Round((((decimal)total.Value / loops) / ((decimal)totalMs / loops)) * 100, 2)}%");
            }

            Console.WriteLine();
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
                _ = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json, newtonsoftConverter);

                _ = Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(obj));
                _ = Utf8Json.JsonSerializer.Deserialize<TypesAllModel>(Encoding.UTF8.GetBytes(json));

                _ = System.Text.Json.JsonSerializer.Serialize(obj, systemTextJsonOptions);
                _ = System.Text.Json.JsonSerializer.Deserialize<TypesAllModel>(json, systemTextJsonOptions);

                _ = JsonSerializerOld.Serialize(obj);
                _ = JsonSerializerOld.Deserialize<TypesAllModel>(json);
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
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} JsonConvert", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = Utf8Json.JsonSerializer.Deserialize<TypesAllModel>(Encoding.UTF8.GetBytes(json));
            }
            timer.Stop();
            Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<TypesAllModel>(json, systemTextJsonOptions);
            }
            timer.Stop();
            Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

            GC.Collect();
            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Deserialize<TypesAllModel>(json);
            }
            timer.Stop();
            Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

            timer = Stopwatch.StartNew();
            for (var i = 0; i < testlength; i++)
            {
                var result = JsonSerializerOld.Deserialize<TypesAllModel>(jsonnameless, optionsNameless);
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
                    var result = await Utf8Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Utf8Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await System.Text.Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream, systemTextJsonOptions);
                }
                timer.Stop();
                Console.WriteLine("{0} System.Text.Json", timer.ElapsedMilliseconds);

                GC.Collect();
                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    stream.Position = 0;
                    var result = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream);
                }
                timer.Stop();
                Console.WriteLine("{0} Zerra.Serialization", timer.ElapsedMilliseconds);

                timer = Stopwatch.StartNew();
                for (var i = 0; i < testlength; i++)
                {
                    streamNameless.Position = 0;
                    var result = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(streamNameless, optionsNameless);
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