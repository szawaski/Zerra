// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Providers;

namespace Zerra.CQRS.Relay
{
    internal class RelayConnectedService
    {
        private const int failureWaitMilliseconds = 5000;
        private const int maxFailures = 5;

        private readonly ConcurrentQueue<long> runtimes = new();

        private readonly object loadLock = new();
        private double averageRuntime = 0;
        private int runtimeCount = 0;
        public double Load
        {
            get
            {
                lock (loadLock)
                {
                    return averageRuntime * CurrentRequests;
                }
            }
        }

        private readonly object requestsLock = new();
        private int currentRequests = 0;
        public int CurrentRequests
        {
            get
            {
                lock (requestsLock)
                {
                    return currentRequests;
                }
            }
        }

        private readonly object failureLock = new();
        private int failures = 0;
        private DateTime lastFailure = DateTime.MinValue;
        public bool Failed
        {
            get
            {
                lock (failureLock)
                {
                    if (failures < maxFailures && lastFailure < DateTime.UtcNow.AddMilliseconds(-failureWaitMilliseconds))
                        return false;
                    return true;
                }
            }
        }

        public string Url { get; set; }

        public void AddStatistic(long runtime)
        {
            if (runtime <= 0)
                return;

            runtimes.Enqueue(runtime);
            lock (loadLock)
            {
                averageRuntime = ((averageRuntime * runtimeCount) + runtime) / (runtimeCount + 1);
                runtimeCount++;
            }
        }

        public void ExpireStatistis(int maxStatistics)
        {
            var numberToRemove = runtimes.Count - maxStatistics;
            if (numberToRemove <= 0)
                return;

            for (var i = 0; i < numberToRemove; i++)
            {
                if (!runtimes.TryDequeue(out var runtime))
                    break;

                lock (loadLock)
                {

                    if (runtimeCount > 1)
                    {
                        averageRuntime = ((averageRuntime * runtimeCount) - runtime) / (runtimeCount - 1);
                        runtimeCount--;
                    }
                    else
                    {
                        averageRuntime = 0;
                        runtimeCount = 0;
                    }
                }
            }
        }

        public Stopwatch StartRequestRunning()
        {
            lock (requestsLock)
            {
                currentRequests++;
            }
            return Stopwatch.StartNew();
        }

        public void EndRequestRunning(Stopwatch requestRunningContext)
        {
            requestRunningContext.Stop();
            _ = Task.Run(() =>
            {
                lock (requestsLock)
                {
                    currentRequests--;
                }
                AddStatistic(requestRunningContext.ElapsedMilliseconds);
            });
        }

        public void FlagConnectionSuccess()
        {
            lock (failureLock)
            {
                failures = 0;
            }
        }
        public void FlagConnectionFailed()
        {
            lock (failureLock)
            {
                if (lastFailure < DateTime.UtcNow.AddMilliseconds(failureWaitMilliseconds))
                {
                    failures++;
                    lastFailure = DateTime.UtcNow;
                }
                if (failures >= maxFailures)
                {
                    RelayConnectedServicesManager.Remove(this.Url);
                    _ = Log.InfoAsync($"Service {this.Url} Failed {maxFailures} times, removing...");
                }
            }
        }
    }
}

#endif