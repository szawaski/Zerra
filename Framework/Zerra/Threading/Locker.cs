// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Threading
{
    public class Locker<T> : IDisposable
    {
        private class ItemLocker
        {
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public int Checkouts = 0;
        }

        private static readonly ConcurrentFactoryDictionary<string, ConcurrentFactoryDictionary<T, ItemLocker>> lockerPools = new ConcurrentFactoryDictionary<string, ConcurrentFactoryDictionary<T, ItemLocker>>();

        private readonly ConcurrentFactoryDictionary<T, ItemLocker> itemLockers;
        private readonly T key;

        private ItemLocker itemLocker;

        private Locker(string purpose, T key)
        {
            this.itemLockers = lockerPools.GetOrAdd(purpose, (k) => { return new ConcurrentFactoryDictionary<T, ItemLocker>(); });
            this.key = key;
        }

        private Task LockAsync()
        {
            lock (itemLockers)
            {
                itemLocker = itemLockers.GetOrAdd(key, (k) => { return new ItemLocker(); });
                lock (itemLocker)
                {
                    itemLocker.Checkouts++;
                }
            }
            return itemLocker.Semaphore.WaitAsync();
        }

        private void Lock()
        {
            lock (itemLockers)
            {
                itemLocker = itemLockers.GetOrAdd(key, (k) => { return new ItemLocker(); });
                lock (itemLocker)
                {
                    itemLocker.Checkouts++;
                }
            }
            itemLocker.Semaphore.Wait();
        }

        public void Dispose()
        {
            itemLocker.Semaphore.Release();

            lock (itemLocker)
            {
                itemLocker.Checkouts--;
                lock (itemLockers)
                {
                    if (itemLocker.Checkouts == 0)
                    {
                        itemLockers.TryRemove(key, out ItemLocker _);
                        itemLocker.Semaphore.Dispose();
                    }
                }
            }
        }

        public static Locker<T> Lock(string purpose, T key)
        {
            if (purpose == null) throw new ArgumentException();
            var locker = new Locker<T>(purpose, key);
            locker.Lock();
            return locker;
        }

        public static async Task<Locker<T>> LockAsync(string purpose, T key)
        {
            if (purpose == null) throw new ArgumentException();
            var locker = new Locker<T>(purpose, key);
            await locker.LockAsync();
            return locker;
        }
    }
}