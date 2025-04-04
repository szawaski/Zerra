// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace System.Threading.Tasks
{
    /// <summary>
    /// Adds functionality to Task.WaitAsync
    /// </summary>
    public static class WaitAsyncExtensions
    {
        /// <summary>
        /// Extends Task.WaitAsync to convert <see cref="ValueTask"/>.
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="it">The <see cref="ValueTask"/> to wait with a timeout.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
        public static Task WaitAsync(this ValueTask it, TimeSpan timeout, CancellationToken cancellationToken = default)
            => it.AsTask().WaitAsync(timeout, cancellationToken);
        /// <summary>
        /// Extends Task.WaitAsync to convert <see cref="ValueTask"/>.
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="it">The <see cref="ValueTask"/> to wait with a timeout.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
        public static Task<T> WaitAsync<T>(this ValueTask<T> it, TimeSpan timeout, CancellationToken cancellationToken = default)
            => it.AsTask().WaitAsync(timeout, cancellationToken);

#if !NET7_0_OR_GREATER
        /// <summary>
        /// Previous .NET support for Task.WaitAsync.
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="it">The <see cref="Task"/> to wait with a timeout.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
        public static async Task WaitAsync(this Task it, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var completedTask = await Task.WhenAny(it, Task.Delay(timeout, cancellationToken));
            if (!ReferenceEquals(completedTask, it))
                throw new TimeoutException();
            if (it.IsFaulted)
                throw it.Exception!; //not null when true
        }
        /// <summary>
        /// Previous .NET support for Task.WaitAsync.
        /// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
        /// </summary>
        /// <param name="it">The <see cref="Task"/> to wait with a timeout.</param>
        /// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous wait.  It may or may not be the same instance as the current instance.</returns>
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