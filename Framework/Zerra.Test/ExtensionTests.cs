﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zerra.Test
{
    [TestClass]
    public class ExtensionTests
    {
        [TestMethod]
        public void StringExtensionsTruncate()
        {
            Assert.AreEqual("1234567890", "1234567890".Truncate(11));
            Assert.AreEqual("1234567890", "1234567890".Truncate(10));
            Assert.AreEqual("123456789", "1234567890".Truncate(9));
            Assert.AreEqual("12345678", "1234567890".Truncate(8));
            Assert.AreEqual("1234567", "1234567890".Truncate(7));
            Assert.AreEqual("1", "1234567890".Truncate(1));
        }

        [TestMethod]
        public void StringExtensionsJoin()
        {
            Assert.AreEqual("1234567890-1234567890", StringExtensions.Join(21, "-", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-123456789", StringExtensions.Join(20, "-", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789", StringExtensions.Join(19, "-", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-12345678", StringExtensions.Join(18, "-", "1234567890", "1234567890"));
            Assert.AreEqual("12345678-12345678", StringExtensions.Join(17, "-", "1234567890", "1234567890"));
            Assert.AreEqual("1-1", StringExtensions.Join(3, "-", "1234567890", "1234567890"));

            Assert.AreEqual("1234567890-1234567890-1234567890", StringExtensions.Join(32, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-1234567890-123456789", StringExtensions.Join(31, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-123456789-123456789", StringExtensions.Join(30, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789-123456789", StringExtensions.Join(29, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789-12345678", StringExtensions.Join(28, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-12345678-12345678", StringExtensions.Join(27, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("12345678-12345678-12345678", StringExtensions.Join(26, "-", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1-1-1", StringExtensions.Join(5, "-", "1234567890", "1234567890", "1234567890"));

            Assert.AreEqual("1234567890-1234567890-1234567890-1234567890", StringExtensions.Join(43, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-1234567890-1234567890-123456789", StringExtensions.Join(42, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-1234567890-123456789-123456789", StringExtensions.Join(41, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1234567890-123456789-123456789-123456789", StringExtensions.Join(40, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789-123456789-123456789", StringExtensions.Join(39, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789-123456789-12345678", StringExtensions.Join(38, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-123456789-12345678-12345678", StringExtensions.Join(37, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("123456789-12345678-12345678-12345678", StringExtensions.Join(36, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("12345678-12345678-12345678-12345678", StringExtensions.Join(35, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
            Assert.AreEqual("1-1-1-1", StringExtensions.Join(7, "-", "1234567890", "1234567890", "1234567890", "1234567890"));
        }

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
