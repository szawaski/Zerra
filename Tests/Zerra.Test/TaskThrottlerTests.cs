// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Threading;

namespace Zerra.Test
{
    public class TaskThrottlerTests
    {
        [Fact]
        public async Task TaskThrottlerTestAsync()
        {
            var list = new ConcurrentReadWriteList<string>();
            await using (var throttler = new TaskThrottler())
            {
                for (var i = 0; i < 100; i++)
                {
                    throttler.Run(async () =>
                    {
                        await Task.Delay(100);
                        list.Add(DateTime.Now.ToString("ss:fff"));
                    });
                }

                await throttler.WaitAsync();

                for (var i = 0; i < 10; i++)
                {
                    throttler.Run(async () =>
                    {
                        await Task.Delay(100);
                        list.Add(DateTime.Now.ToString("ss:fff"));
                    });
                }

                await throttler.WaitAsync();
            }
        }

        [Fact]
        public async Task TaskThrottlerForEachTestAsync()
        {
            var list = new ConcurrentReadWriteList<string>();

            var items = new int[100];

            await TaskThrottler.ForEachAsync(items, async (item, cancellationToken) =>
            {
                await Task.Delay(100);
                list.Add(DateTime.Now.ToString("ss:fff"));
            });
        }
    }
}
