// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Test
{
    public interface ITestProvider { }

    public class TestRuleProvider : ITestProvider, IRuleProvider { }
    public class TestCacheProvider : ITestProvider, ICacheProvider { }
    //public class TestEncryptionProvider : ITestProvider, IEncryptionProvider { } should be skipped
    public class TestCompressionProvider : ITestProvider, ICompressionProvider, IIgnoreProviderResolver { } //should be skipped
    public class TestDualBaseProvider : ITestProvider, IDualBaseProvider { }
    public class TestProvider : ITestProvider { }
    public class TestBusCacheProvider : ITestProvider, IIgnoreProviderResolver { } //should not throw duplicate error

    public interface ITestDuplicateProvider { }
    public class TestDuplicate1Provider : ITestDuplicateProvider { }
    public class TestDuplicate2Provider : ITestDuplicateProvider { }

    [TestClass]
    public class DiscoveryTest
    {
        private static int GetInterfaceIndex(Type interfaceType)
        {
            var i = 0;
            for (; i < ProviderResolver.InterfaceStack.Count; i++)
            {
                if (ProviderResolver.InterfaceStack[i] == interfaceType)
                    return i;
            }
            return i;
        }

        [TestMethod]
        public void ProviderResolverTraverseLayers()
        {
            var providerType = ProviderResolver.GetTypeFirst(typeof(ITestProvider));

            while (true)
            {
                var nextProviderType = ProviderResolver.GetNextType<ITestProvider>(providerType);
                if (nextProviderType is null)
                    break;

                providerType = nextProviderType;
            }
        }

        [TestMethod]
        public void Duplicates()
        {
            var topLayerProvider = Discovery.GetClassByInterface(typeof(ITestDuplicateProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface, false);
            Assert.IsNull(topLayerProvider);
        }

        [TestMethod]
        public void Layers()
        {
            var topLayerProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.AreEqual(typeof(TestRuleProvider), topLayerProvider);

            var ruleProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface, false);
            Assert.AreEqual(typeof(TestDualBaseProvider), compressionProvider); //Compression Provider has IIgnoreProviderResolver

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);

            var provider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(typeof(TestProvider), provider);
        }

        [TestMethod]
        public void HasLayers()
        {
            var topLayerProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.IsTrue(topLayerProvider);

            var ruleProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(cacheProvider);

            var encryptionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IEncryptionProvider)), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(encryptionProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(dualbaseProvider);

            var provider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.IsTrue(provider);
        }

        [TestMethod]
        public void ManyLayers()
        {
            var topLayerProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, topLayerProvider.Count);
            Assert.IsTrue(topLayerProvider.Contains(typeof(TestRuleProvider)));

            var ruleProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, ruleProvider.Count);
            Assert.IsTrue(ruleProvider.Contains(typeof(TestRuleProvider)));

            var cacheProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, cacheProvider.Count);
            Assert.IsTrue(cacheProvider.Contains(typeof(TestCacheProvider)));

            var compressionProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, compressionProvider.Count);  //Compression Provider has IIgnoreProviderResolver
            Assert.IsTrue(compressionProvider.Contains(typeof(TestDualBaseProvider))); //Compression Provider has IIgnoreProviderResolver

            var dualbaseProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, dualbaseProvider.Count);
            Assert.IsTrue(dualbaseProvider.Contains(typeof(TestDualBaseProvider)));

            var provider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.AreEqual(1, provider.Count);
            Assert.IsTrue(provider.Contains(typeof(TestProvider)));
        }

        [TestMethod]
        public void SecondaryInterface()
        {
            var ruleProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.AreEqual(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [TestMethod]
        public void HasSecondaryInterface()
        {
            var ruleProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.IsTrue(cacheProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.IsTrue(dualbaseProvider);
        }

        [TestMethod]
        public void ManySecondaryInterface()
        {
            var providers = Discovery.GetClassesByInterface(typeof(ITestProvider));
            Assert.AreEqual(6, providers.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCacheProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCompressionProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetClassesByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.AreEqual(1, ruleProviders.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
        }

        [TestMethod]
        public void SingleInterface()
        {
            var ruleProvider = Discovery.GetClassByInterface(typeof(IRuleProvider));
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ICacheProvider));
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ICompressionProvider));
            Assert.AreEqual(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(IDualBaseProvider));
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [TestMethod]
        public void HasSingleInterface()
        {
            var ruleProvider = Discovery.HasClassByInterface(typeof(IRuleProvider));
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ICacheProvider));
            Assert.IsTrue(cacheProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ICompressionProvider));
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(IDualBaseProvider));
            Assert.IsTrue(dualbaseProvider);
        }

        [TestMethod]
        public void ManySingleInterface()
        {
            var providers = Discovery.GetClassesByInterface(typeof(ITestProvider));
            Assert.AreEqual(6, providers.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCacheProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCompressionProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetClassesByInterface(typeof(IRuleProvider));
            Assert.AreEqual(1, ruleProviders.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
        }
    }
}
