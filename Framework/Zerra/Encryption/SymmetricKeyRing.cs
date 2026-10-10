// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

namespace Zerra.Encryption
{
    //the current and previous keys from an ISymmetricKeySource, shared by everything one encryptor encrypts and decrypts
    internal sealed class SymmetricKeyRing
    {
        private static readonly TimeSpan failedFetchInterval = TimeSpan.FromMinutes(1);

        private readonly ISymmetricKeySource source;
        private readonly SemaphoreSlim fetchLock = new(1, 1);
        private State? state;
        private DateTime lastFailedFetch = DateTime.MinValue;

        public sealed class State
        {
            public readonly byte[] Current;
            public readonly byte[] CurrentCheck;
            public readonly DateTime Expiration;
            public readonly byte[]? Previous;
            public readonly byte[]? PreviousCheck;

            public State(byte[] current, byte[] currentCheck, DateTime expiration, byte[]? previous, byte[]? previousCheck)
            {
                Current = current;
                CurrentCheck = currentCheck;
                Expiration = expiration;
                Previous = previous;
                PreviousCheck = previousCheck;
            }
        }

        public SymmetricKeyRing(ISymmetricKeySource source)
        {
            this.source = source;
        }

        public State GetState()
        {
            var current = Volatile.Read(ref state);
            if (current is not null && DateTime.UtcNow < current.Expiration)
                return current;

            fetchLock.Wait();
            try
            {
                current = state;
                if (current is not null && DateTime.UtcNow < current.Expiration)
                    return current;
                return Apply(source.GetKey());
            }
            finally
            {
                _ = fetchLock.Release();
            }
        }

        public async ValueTask<State> GetStateAsync()
        {
            var current = Volatile.Read(ref state);
            if (current is not null && DateTime.UtcNow < current.Expiration)
                return current;

            await fetchLock.WaitAsync();
            try
            {
                current = state;
                if (current is not null && DateTime.UtcNow < current.Expiration)
                    return current;
                return Apply(await source.GetKeyAsync());
            }
            finally
            {
                _ = fetchLock.Release();
            }
        }

        //the key whose check starts the data: the current one, the previous one, or a newer one from the source when the sender moved first
        public byte[] SelectKey(ReadOnlySpan<byte> keyCheck)
        {
            var current = GetState();
            if (keyCheck.SequenceEqual(current.CurrentCheck))
                return current.Current;
            if (current.PreviousCheck is not null && keyCheck.SequenceEqual(current.PreviousCheck))
                return current.Previous!;

            fetchLock.Wait();
            try
            {
                if (!TryGetNewer(current, out var newer))
                    newer = Apply(source.GetKey());
                if (newer is not null && keyCheck.SequenceEqual(newer.CurrentCheck))
                    return newer.Current;
            }
            finally
            {
                _ = fetchLock.Release();
            }
            throw new KeyMismatchException();
        }

        public async ValueTask<byte[]> SelectKeyAsync(byte[] keyCheck)
        {
            var current = await GetStateAsync();
            if (keyCheck.AsSpan().SequenceEqual(current.CurrentCheck))
                return current.Current;
            if (current.PreviousCheck is not null && keyCheck.AsSpan().SequenceEqual(current.PreviousCheck))
                return current.Previous!;

            await fetchLock.WaitAsync();
            try
            {
                if (!TryGetNewer(current, out var newer))
                    newer = Apply(await source.GetKeyAsync());
                if (newer is not null && keyCheck.AsSpan().SequenceEqual(newer.CurrentCheck))
                    return newer.Current;
            }
            finally
            {
                _ = fetchLock.Release();
            }
            throw new KeyMismatchException();
        }

        //true when nothing is fetched: another caller already fetched a newer key, given back, or the last failed fetch was too recent
        private bool TryGetNewer(State tried, out State? newer)
        {
            var current = state!;
            if (current.Current != tried.Current)
            {
                newer = current;
                return true;
            }
            newer = null;
            var now = DateTime.UtcNow;
            if (now - lastFailedFetch < failedFetchInterval)
                return true;
            lastFailedFetch = now;
            return false;
        }

        //a key the same as the current one only moves the expiration, so the previous key isn't lost
        private State Apply((byte[] Key, DateTime Expiration) fetched)
        {
            if (fetched.Key is null)
                throw new InvalidOperationException($"{nameof(ISymmetricKeySource)} returned no key");
            if (fetched.Key.Length is not (16 or 24 or 32))
                throw new CryptographicException($"{nameof(ISymmetricKeySource)} returned a key of {fetched.Key.Length} bytes, it must be 16, 24, or 32");
            var expiration = fetched.Expiration.Kind == DateTimeKind.Local ? fetched.Expiration.ToUniversalTime() : fetched.Expiration;

            var current = state;
            State next;
            if (current is null)
            {
                var key = (byte[])fetched.Key.Clone();
                next = new State(key, CryptoChunkStream.GetKeyCheck(key), expiration, null, null);
            }
            else if (current.Current.AsSpan().SequenceEqual(fetched.Key))
            {
                next = new State(current.Current, current.CurrentCheck, expiration, current.Previous, current.PreviousCheck);
            }
            else
            {
                var key = (byte[])fetched.Key.Clone();
                next = new State(key, CryptoChunkStream.GetKeyCheck(key), expiration, current.Current, current.CurrentCheck);
            }
            Volatile.Write(ref state, next);
            return next;
        }
    }
}
