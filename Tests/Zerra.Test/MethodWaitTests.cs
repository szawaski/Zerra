// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;
using System.Threading;

namespace Zerra.Test
{
    public class MethodWaitTests
    {
        [Fact]
        public void WaitActionTimeout()
        {
            TimeoutException exception = null;
            try
            {
                MethodWait.Wait(() => DoSomethingAction(TimeSpan.FromMilliseconds(1000)), TimeSpan.FromMilliseconds(500));
            }
            catch (TimeoutException ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
        }

        [Fact]
        public void WaitActionCompletion()
        {
            MethodWait.Wait(() => DoSomethingAction(TimeSpan.FromMilliseconds(500)), TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public void WaitActionException()
        {
            ArgumentException exception = null;
            try
            {
                MethodWait.Wait(() => DoSomethingAction(TimeSpan.FromMilliseconds(-500)), TimeSpan.FromMilliseconds(5000));
            }
            catch (ArgumentException ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
        }

        [Fact]
        public void WaitFuncTimeout()
        {
            TimeoutException exception = null;
            try
            {
                _ = MethodWait.Wait(() => DoSomethingFunc(TimeSpan.FromMilliseconds(1000)), TimeSpan.FromMilliseconds(500));
            }
            catch (TimeoutException ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
        }

        [Fact]
        public void WaitFuncCompletion()
        {
            var result = MethodWait.Wait(() => DoSomethingFunc(TimeSpan.FromMilliseconds(500)), TimeSpan.FromMilliseconds(1000));
            Assert.True(result);
        }

        [Fact]
        public void WaitFuncException()
        {
            ArgumentException exception = null;
            try
            {
                _ = MethodWait.Wait(() => DoSomethingFunc(TimeSpan.FromMilliseconds(-500)), TimeSpan.FromMilliseconds(5000));
            }
            catch (ArgumentException ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
        }

        private static void DoSomethingAction(TimeSpan timer)
        {
            if (timer.TotalMilliseconds < 0)
                throw new ArgumentException();
            Thread.Sleep(timer);
        }

        private static bool DoSomethingFunc(TimeSpan timer)
        {
            if (timer.TotalMilliseconds < 0)
                throw new ArgumentException();
            Thread.Sleep(timer);
            return true;
        }
    }
}
