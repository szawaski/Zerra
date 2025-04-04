// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.TestDev
{
    public static class TestTaskRun
    {
        public static async Task Test()
        {
            Console.WriteLine("Call Method Await (Regular)");
            await DoThings();
            Console.WriteLine("Resumed");

            await Task.Delay(500);
            Console.WriteLine();

            Console.WriteLine("Call Method Discard Await");
            _ = DoThings();
            Console.WriteLine("Resumed");

            await Task.Delay(500);
            Console.WriteLine();

            Console.WriteLine("Task Run Discard Await");
            _ = Task.Run(DoThings);
            Console.WriteLine("Resumed");

            await Task.Delay(500);
            Console.WriteLine();
        }

        private static async Task DoThings()
        {
            Console.WriteLine("Step 1");
            Console.WriteLine("Step 2");
            Console.WriteLine("Step 3");
            await Task.Delay(250);
            Console.WriteLine("Step 4");
            Console.WriteLine("Step 5");
            Console.WriteLine("Step 6");
        }
    }
}
