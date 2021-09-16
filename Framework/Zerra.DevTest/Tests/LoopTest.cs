// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Zerra.DevTest
{
    public static class LoopTest
    {
        public static void Test()
        {
            var array = new int[500000000];

            var addMe = 0;

            //warmup
            var timer0 = Stopwatch.StartNew();
            for (var i = 0; i < array.Length; i++) { var item = array[i]; addMe += item + 1; }
            for (var i = 0; i < array.Length; i++) { var item = array[i]; addMe += item + 1; }
            timer0.Stop();
            Console.WriteLine(timer0.ElapsedMilliseconds + " Warmup");

            addMe = 0;
            var timer1 = Stopwatch.StartNew();
            for (var i = 0; i < array.Length; i++) { var item = array[i]; addMe += item + 1; }
            timer1.Stop();
            Console.WriteLine(timer1.ElapsedMilliseconds + " Normal");

            addMe = 0;
            var timer1a = Stopwatch.StartNew();
            var index = 0;
            while (index < array.Length) { var item = array[index]; addMe += item + 1; index++; }
            timer1a.Stop();
            Console.WriteLine(timer1a.ElapsedMilliseconds + " While");

            addMe = 0;
            var timer1b = Stopwatch.StartNew();
            var length = array.Length;
            for (var i = 0; i < length; i++) { var item = array[i]; addMe += item + 1; }
            timer1b.Stop();
            Console.WriteLine(timer1b.ElapsedMilliseconds + " Length Variable");

            addMe = 0;
            var timer1c = Stopwatch.StartNew();
            var start = 1;
            for (var i = start; i < array.Length; i++) { var item = array[i]; addMe += item + 1; }
            timer1c.Stop();
            Console.WriteLine(timer1c.ElapsedMilliseconds + " Start Variable");

            addMe = 0;
            var timer1d = Stopwatch.StartNew();
            index = 0;
            length = array.Length / 2;
#pragma warning disable CS1717 // Assignment made to same variable
            for (index = index; index < length; index++) { var item = array[index]; addMe += item + 1; }
#pragma warning restore CS1717 // Assignment made to same variable
#pragma warning disable CS1717 // Assignment made to same variable
            for (index = index; index < array.Length; index++) { var item = array[index]; addMe += item + 1; }
#pragma warning restore CS1717 // Assignment made to same variable
            timer1d.Stop();
            Console.WriteLine(timer1d.ElapsedMilliseconds + " Broken");

            addMe = 0;
            var timer2 = Stopwatch.StartNew();
            foreach (var item in array) { addMe += item + 1; };
            timer2.Stop();
            Console.WriteLine(timer2.ElapsedMilliseconds + " foreach");

            addMe = 0;
            var enumerator = ((IEnumerable<int>)array).GetEnumerator();
            var timer3 = Stopwatch.StartNew();
            while (enumerator.MoveNext()) { var item = enumerator.Current; addMe += item + 1; };
            timer3.Stop();
            Console.WriteLine(timer3.ElapsedMilliseconds + " Enumerator");
        }
    }
}
