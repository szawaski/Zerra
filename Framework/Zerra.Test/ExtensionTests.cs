// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zerra.Test
{
    [TestClass]
    public class ExtensionTests
    {
        [TestMethod]
        public void StringExtensionsMatchWildcard()
        {
            var test1 = "C:\\folder1\\folder2\\file.ext";

            Assert.IsTrue(test1.MatchWildcard("*\\folder2*"));
            Assert.IsTrue(test1.MatchWildcard("C:\\folder1\\*"));
            Assert.IsTrue(test1.MatchWildcard("*\\file.ext"));
            Assert.IsTrue(test1.MatchWildcard("C:\\*der1\\*der2\\*.ext"));
            Assert.IsTrue(test1.MatchWildcard("*der1*der2*file*"));

            Assert.IsFalse(test1.MatchWildcard("\\folder2*"));
            Assert.IsFalse(test1.MatchWildcard("*\\folder2"));
            Assert.IsFalse(test1.MatchWildcard("*der1*stuff*file*"));

            Assert.IsTrue(((string)null).MatchWildcard("*"));
            Assert.IsTrue(string.Empty.MatchWildcard("*"));

            Assert.IsFalse(((string)null).MatchWildcard("stuff"));
            Assert.IsFalse(string.Empty.MatchWildcard("stuff"));
        }
    }
}
