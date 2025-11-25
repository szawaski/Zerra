// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.CQRS;
using System;
using System.Threading;

namespace Zerra.TestDev
{
    public static class MSILTest
    {
        public static T ReturnDefault<T>()
        {
            return default;
        }

        public interface IStuff
        {
            int prop { get; set; }
        }

        public class Stuff : IStuff
        {
            public int prop { get; set; }
            public int field;

            public int DoThings(int addme)
            {
                prop += addme;
                return prop;
            }

            public void Something(object[] args)
            {
                var length = args is null ? 0 : args.Length;
            }

            public static int Other(int arg)
            {
                return arg;
            }
        }

        public class TestClass
        {
            public IEqualityComparer<string> Test1(object stuff) => ((Dictionary<string, string>)stuff).Comparer;
        }

        public static void Test()
        {
            var stuff = new Stuff() { prop = 4, field = 5 };

            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Stuff));
            var item1 = (int)typeDetail.MemberDetails[0].GetterBoxed(stuff);
            var item2 = (int)typeDetail.MemberDetails[1].GetterBoxed(stuff);
            typeDetail.MemberDetails[0].SetterBoxed(stuff, item1 * 2);
            typeDetail.MemberDetails[1].SetterBoxed(stuff, item2 * 2);

            //var item3 = (int)typeDetails.GetMethod("DoThings").Caller(stuff, new object[] { 5 });
            //var item3a = (int)typeDetails.GetMethod("Other").Caller(null, new object[] { 5 });

            var item4 = typeDetail.ConstructorDetailsBoxed[0].CreatorWithArgsBoxed(null);

            var item5 = Instantiator.Create<Stuff>();
        }

        //public static void TestSpeed()
        //{
        //    var model = new Cookie();
        //    var property = nameof(Cookie.Comment);
        //    var value = "1";

        //    const int itterations = 30000000;
        //    var accessor1 = MemberAccessor.Get(model.GetType());
        //    var accessor2 = TypeAccessor.Create(model.GetType());

        //    var timer = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor1[model, property] = value;
        //        accessor2[model, property] = value;
        //    }
        //    timer.Stop();
        //    Console.WriteLine($"Warmup {timer.ElapsedMilliseconds}ms");

        //    var timer1 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor1[model, property] = value;
        //        var read = accessor1[model, property];
        //    }
        //    timer1.Stop();
        //    Console.WriteLine($"PropertyAccessor {timer1.ElapsedMilliseconds}ms");

        //    var timer2 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor2[model, property] = value;
        //        var read = accessor1[model, property];
        //    }
        //    timer2.Stop();
        //    Console.WriteLine($"TypeAccessor {timer2.ElapsedMilliseconds}ms");

        //    var timer4 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor1[model, property] = value;
        //        var read = accessor1[model, property];
        //    }
        //    timer4.Stop();
        //    Console.WriteLine($"PropertyAccessor {timer4.ElapsedMilliseconds}ms");

        //    var timer3 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor2[model, property] = value;
        //        var read = accessor1[model, property];
        //    }
        //    timer3.Stop();
        //    Console.WriteLine($"TypeAccessor {timer3.ElapsedMilliseconds}ms");

        //    var accessor1Index = accessor1[property];

        //    var timer5 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor1[model, accessor1Index] = value;
        //        var read = accessor1[model, accessor1Index];
        //    }
        //    timer5.Stop();
        //    Console.WriteLine($"PropertyAccessor Indexed {timer5.ElapsedMilliseconds}ms");

        //    var timer6 = Stopwatch.StartNew();
        //    for (var i = 0; i < itterations; i++)
        //    {
        //        accessor2[model, property] = value;
        //        var read = accessor1[model, property];
        //    }
        //    timer6.Stop();
        //    Console.WriteLine($"TypeAccessor {timer6.ElapsedMilliseconds}ms");
        //}


        public interface ICallThings
        {
            Task<Stuff> GetThings(Type interfaceType, string methodName, object[] arguments, string source);
        }

        public sealed class CallThings
        {
            private readonly ICallThings bus;
            private readonly string source;
            public CallThings(ICallThings bus, string source)
            {
                this.bus = bus;
                this.source = source;
            }

            public Task<Stuff> GetThings(int arg1, string arg2)
            {
                return bus.GetThings(typeof(ICallThings), "Boop", [arg1, arg2], source);
            }
        }
    }
}
