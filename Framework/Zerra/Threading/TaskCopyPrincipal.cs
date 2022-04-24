// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.Threading
{
    public static class TaskCopyPrincipal
    {
        public static Task Run(Action function)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                function.Invoke();
            });
        }
        public static Task Run(Action function, CancellationToken cancellationToken)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                function.Invoke();
            }, cancellationToken);
        }
        public static Task Run(Func<Task> function)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            });
        }
        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            }, cancellationToken);
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            });
        }
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            }, cancellationToken);
        }
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            });
        }
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            var principal = Thread.CurrentPrincipal.CopyClaimsPrincipal();
            return Task.Run(() =>
            {
                Thread.CurrentPrincipal = principal;
                return function.Invoke();
            }, cancellationToken);
        }
    }
}
