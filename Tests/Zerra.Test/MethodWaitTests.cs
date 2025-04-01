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
        public void WaitTimeout()
        {
            Exception exception = null;
            try
            {
                _ = MethodWait.Wait(DoSomething, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(500));
            }
            catch (TimeoutException ex)
            {
                exception = ex;
            }
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void WaitCompletion()
        {
            var result = MethodWait.Wait(DoSomething, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WaitException()
        {
            Exception exception = null;
            try
            {
                _ = MethodWait.Wait(DoSomething, TimeSpan.FromMilliseconds(-500), TimeSpan.FromMilliseconds(5000));
            }
            catch (ArgumentException ex)
            {
                exception = ex;
            }
            Assert.IsNotNull(exception);
        }

        private bool DoSomething(TimeSpan timer)
        {
            if (timer.TotalMilliseconds < 0)
                throw new ArgumentException();
            Thread.Sleep(timer);
            return true;
        }
    }
}
