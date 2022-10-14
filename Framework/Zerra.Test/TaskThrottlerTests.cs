// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Threading;

namespace Zerra.Test
{
    [TestClass]
    public class TaskThrottlerTests
    {
        [TestMethod]
        public void TaskThrottlerTest() { TaskThrottlerTestAsync().GetAwaiter().GetResult(); }
        public async Task TaskThrottlerTestAsync()
        {
            var list = new ConcurrentReadWriteList<string>();
            await using (var throttler = new TaskThrottler())
            {
                for (var i = 0; i < 100; i++)
                {
                    throttler.Run(async () =>
                    {
                        await Task.Delay(1000);
                        list.Add(DateTime.Now.ToString("ss:fff"));
                    });
                }

                await throttler.WaitAsync();

                for (var i = 0; i < 10; i++)
                {
                    throttler.Run(async () =>
                    {
                        await Task.Delay(1000);
                        list.Add(DateTime.Now.ToString("ss:fff"));
                    });
                }

                await throttler.WaitAsync();
            }
        }

        [TestMethod]
        public void TaskThrottlerForEachTest() { TaskThrottlerForEachTestAsync().GetAwaiter().GetResult(); }
        public async Task TaskThrottlerForEachTestAsync()
        {
            var list = new ConcurrentReadWriteList<string>();

            var items = new int[100];
            
            await TaskThrottler.ForEachAsync(items, async (item, cancellationToken) =>
            {
                await Task.Delay(1000);
                list.Add(DateTime.Now.ToString("ss:fff"));
            });
        }
    }
}
