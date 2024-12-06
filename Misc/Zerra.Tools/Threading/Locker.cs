// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Threading
{
    public sealed class Locker<T> : IDisposable
        where T : notnull
    {
        private sealed class ItemLocker
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public int Checkouts = 0;
        }

        private static readonly ConcurrentFactoryDictionary<string, Dictionary<T, ItemLocker>> lockerPools = new();

        private readonly Dictionary<T, ItemLocker> itemLockers;
        private readonly ItemLocker itemLocker;
        private readonly T key;

        private bool disposed = false;

        private Locker(string purpose, T key)
        {
            this.itemLockers = lockerPools.GetOrAdd(purpose, static () => new Dictionary<T, ItemLocker>());
            lock (itemLockers)
            {
                if (!itemLockers.TryGetValue(key, out var itemLocker))
                {
                    itemLocker = new ItemLocker();
                    itemLockers.Add(key, itemLocker);
                }
                this.itemLocker = itemLocker;
                itemLocker.Checkouts++;
            }
            this.key = key;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            _ = itemLocker.Semaphore.Release();

            lock (itemLockers)
            {
                itemLocker.Checkouts--;
                if (itemLocker.Checkouts == 0)
                {
                    _ = itemLockers.Remove(key);
                    itemLocker.Semaphore.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        public static Locker<T> Lock(string purpose, T key)
        {
            if (purpose is null)
                throw new ArgumentException();
            var locker = new Locker<T>(purpose, key);
            locker.itemLocker.Semaphore.Wait();
            return locker;
        }

        public static async Task<Locker<T>> LockAsync(string purpose, T key)
        {
            if (purpose is null)
                throw new ArgumentNullException(nameof(purpose));
            var locker = new Locker<T>(purpose, key);
            await locker.itemLocker.Semaphore.WaitAsync();
            return locker;
        }
    }
}