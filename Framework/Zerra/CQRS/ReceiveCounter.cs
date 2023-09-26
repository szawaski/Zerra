// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;

namespace Zerra.CQRS
{
    public sealed class ReceiveCounter
    {
        private readonly object locker = new();

        private int started = 0;
        private int completed = 0;

        private readonly int? maxReceive;
        private readonly Action processExit;
        public ReceiveCounter()
        {
            this.maxReceive = null;
            this.processExit = null;
        }
        public ReceiveCounter(int? maxReceive, Action processExit)
        {
            if (maxReceive.HasValue && maxReceive.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(maxReceive));

            this.maxReceive = maxReceive;
            this.processExit = processExit;
        }

        public int? MaxReceive => maxReceive;

        public bool BeginReceive()
        {
            if (!maxReceive.HasValue)
                return true;

            lock (locker)
            {
                if (started == maxReceive.Value)
                    return false; //do not receive any more
                started++;
            }
            return true;
        }

        public void CompleteReceive(SemaphoreSlim throttle)
        {
            if (!maxReceive.HasValue)
            {
                throttle.Release();
                return;
            }

            lock (locker)
            {
                completed++;
                if (completed == maxReceive.Value)
                    processExit?.Invoke();
                else if (throttle.CurrentCount < maxReceive.Value - started)
                    _ = throttle.Release(); //do not release more than needed to reach maxReceive
            }
        }
    }
}