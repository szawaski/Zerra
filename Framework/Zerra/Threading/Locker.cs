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
        private sealed class ItemLocker
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public int Checkouts = 0;
        }

        private static readonly ConcurrentFactoryDictionary<string, ConcurrentFactoryDictionary<T, ItemLocker>> lockerPools = new();

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
            _ = itemLocker.Semaphore.Release();

            lock (itemLocker)
            {
                itemLocker.Checkouts--;
                lock (itemLockers)
                {
                    if (itemLocker.Checkouts == 0)
                    {
                        _ = itemLockers.TryRemove(key, out _);
                        itemLocker.Semaphore.Dispose();
                    }
                }
            }
            GC.SuppressFinalize(this);
        }

        public static Locker<T> Lock(string purpose, T key)
        {
            if (purpose == null)
                throw new ArgumentException();
            var locker = new Locker<T>(purpose, key);
            locker.Lock();
            return locker;
        }

        public static async Task<Locker<T>> LockAsync(string purpose, T key)
        {
            if (purpose == null)
                throw new ArgumentException();
            var locker = new Locker<T>(purpose, key);
            await locker.LockAsync();
            return locker;
        }
    }
}