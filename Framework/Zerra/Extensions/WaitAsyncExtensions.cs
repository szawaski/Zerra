// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace System.Threading.Tasks
{
    public static class WaitAsyncExtensions
    {
        public static Task WaitAsync(this ValueTask it, TimeSpan timeout, CancellationToken cancellationToken = default)
            => it.AsTask().WaitAsync(timeout, cancellationToken);
        public static Task<T> WaitAsync<T>(this ValueTask<T> it, TimeSpan timeout, CancellationToken cancellationToken = default)
            => it.AsTask().WaitAsync(timeout, cancellationToken);

#if !NET7_0_OR_GREATER
        public static async Task WaitAsync(this Task it, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var completedTask = await Task.WhenAny(it, Task.Delay(timeout, cancellationToken));
            if (!ReferenceEquals(completedTask, it))
                throw new TimeoutException();
            if (it.IsFaulted)
                throw it.Exception!; //not null when true
        }
        public static async Task<T> WaitAsync<T>(this Task<T> it, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var completedTask = await Task.WhenAny(it, Task.Delay(timeout, cancellationToken));
            if (!ReferenceEquals(completedTask, it))
                throw new TimeoutException();
            if (it.IsFaulted)
                throw it.Exception!; //not null when true
            return it.Result;
        }
#endif
    }
}