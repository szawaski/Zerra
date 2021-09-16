// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Zerra.DevTest
{
    public static class InlineTest
    {
        public static  void Test()
        {
            const int loops = 1000000000;

            for (var i = 0; i < loops; i++)
            {
                AddMe1(5);
                AddMe2(5);
            }

            var timer1 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
            {
                var test = AddMe1(5);
            }
            timer1.Stop();
            Console.WriteLine($"{timer1.ElapsedMilliseconds}");

            var timer2 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
            {
                var test = AddMe2(5);
            }
            timer2.Stop();
            Console.WriteLine($"{timer2.ElapsedMilliseconds}");

            var timer3 = Stopwatch.StartNew();
            for (var i = 0; i < loops; i++)
            {

#pragma warning disable CS0219 // Variable is assigned but its value is never used
                var newstuff = 5 + 5;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            }
            timer3.Stop();
            Console.WriteLine($"{timer3.ElapsedMilliseconds}");
        }

        public static int AddMe1(int stuff)
        {
            var newstuff = stuff + 5;
            return newstuff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddMe2(int stuff)
        {
            var newstuff = stuff + 5;
            return newstuff;
        }
    }
}
