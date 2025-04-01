// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace Zerra.Test
{
    [TestClass]
    public class MethodWaitTests
    {
        [TestMethod]
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
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void WaitActionCompletion()
        {
            MethodWait.Wait(() => DoSomethingAction(TimeSpan.FromMilliseconds(500)), TimeSpan.FromMilliseconds(1000));
        }

        [TestMethod]
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
            Assert.IsNotNull(exception);
        }

        [TestMethod]
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
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void WaitFuncCompletion()
        {
            var result = MethodWait.Wait(() => DoSomethingFunc(TimeSpan.FromMilliseconds(500)), TimeSpan.FromMilliseconds(1000));
            Assert.IsTrue(result);
        }

        [TestMethod]
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
            Assert.IsNotNull(exception);
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
