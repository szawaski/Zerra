// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;

namespace Zerra.CQRS
{
    public sealed class CommandCounter
    {
        private readonly object locker = new();

        private int started = 0;
        private int completed = 0;

        private readonly int? receiveCountBeforeExit;
        private readonly Action processExit;
        public CommandCounter()
        {
            this.receiveCountBeforeExit = null;
            this.processExit = null;
        }
        public CommandCounter(int? receiveCountBeforeExit, Action processExit)
        {
            if (receiveCountBeforeExit.HasValue && receiveCountBeforeExit.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(receiveCountBeforeExit));

            this.receiveCountBeforeExit = receiveCountBeforeExit;
            this.processExit = processExit;
        }

        public int? ReceiveCountBeforeExit => receiveCountBeforeExit;

        public bool BeginReceive()
        {
            if (!receiveCountBeforeExit.HasValue)
                return true;

            lock (locker)
            {
                if (started == receiveCountBeforeExit.Value)
                    return false; //do not receive any more
                started++;
            }
            return true;
        }

        public void CancelReceive(SemaphoreSlim throttle)
        {
            if (!receiveCountBeforeExit.HasValue)
            {
                throttle.Release();
                return;
            }

            lock (locker)
            {
                started--;
                _ = throttle.Release();
            }
        }

        public void CompleteReceive(SemaphoreSlim throttle)
        {
            if (!receiveCountBeforeExit.HasValue)
            {
                throttle.Release();
                return;
            }

            lock (locker)
            {
                completed++;
                if (completed == receiveCountBeforeExit.Value)
                    processExit?.Invoke();
                else if (throttle.CurrentCount < receiveCountBeforeExit.Value - started)
                    _ = throttle.Release(); //do not release more than needed to reach maxReceive
            }
        }
    }
}