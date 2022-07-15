// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Test
{
    public interface ITestProvider : IBaseProvider { }

    public class TestRuleProvider : ITestProvider, IRuleProvider { }
    public class TestCacheProvider : ITestProvider, ICacheProvider { }
    //public class TestEncryptionProvider : ITestProvider, IEncryptionProvider { }
    public class TestCompressionProvider : ITestProvider, ICompressionProvider { }
    public class TestDualBaseProvider : ITestProvider, IDualBaseProvider { }
    public class TestProvider : ITestProvider { }

    public interface ITestDuplicateProvider : IBaseProvider { }
    public class TestDuplicate1Provider : ITestDuplicateProvider { }
    public class TestDuplicate2Provider : ITestDuplicateProvider { }

    [TestClass]
    public class DiscoveryTest
    {
        [TestMethod]
        public void Duplicates()
        {
            var topLayerProvider = Discovery.GetImplementationType(typeof(ITestDuplicateProvider), ProviderLayers.GetProviderInterfaceStack(), 0, false);
            Assert.IsNull(topLayerProvider);
        }

        [TestMethod]
        public void Layers()
        {
            var topLayerProvider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(), 0);
            Assert.AreEqual(typeof(TestRuleProvider), topLayerProvider);

            var ruleProvider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IRuleProvider)));
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICacheProvider)));
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICompressionProvider)));
            Assert.AreEqual(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IDualBaseProvider)));
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);

            var provider = Discovery.GetImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IBaseProvider)));
            Assert.AreEqual(typeof(TestProvider), provider);
        }

        [TestMethod]
        public void HasLayers()
        {
            var topLayerProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(), 0);
            Assert.IsTrue(topLayerProvider);

            var ruleProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IRuleProvider)));
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICacheProvider)));
            Assert.IsTrue(cacheProvider);

            var encryptionProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IEncryptionProvider)));
            Assert.IsTrue(encryptionProvider);

            var compressionProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICompressionProvider)));
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IDualBaseProvider)));
            Assert.IsTrue(dualbaseProvider);

            var provider = Discovery.HasImplementationType(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IBaseProvider)));
            Assert.IsTrue(provider);
        }

        [TestMethod]
        public void ManyLayers()
        {
            var topLayerProvider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(), 0);
            Assert.AreEqual(1, topLayerProvider.Count);
            Assert.IsTrue(topLayerProvider.Contains(typeof(TestRuleProvider)));

            var ruleProvider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IRuleProvider)));
            Assert.AreEqual(1, ruleProvider.Count);
            Assert.IsTrue(ruleProvider.Contains(typeof(TestRuleProvider)));

            var cacheProvider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICacheProvider)));
            Assert.AreEqual(1, cacheProvider.Count);
            Assert.IsTrue(cacheProvider.Contains(typeof(TestCacheProvider)));

            var compressionProvider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(ICompressionProvider)));
            Assert.AreEqual(1, compressionProvider.Count);
            Assert.IsTrue(compressionProvider.Contains(typeof(TestCompressionProvider)));

            var dualbaseProvider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IDualBaseProvider)));
            Assert.AreEqual(1, dualbaseProvider.Count);
            Assert.IsTrue(dualbaseProvider.Contains(typeof(TestDualBaseProvider)));

            var provider = Discovery.GetImplementationTypes(typeof(ITestProvider), ProviderLayers.GetProviderInterfaceStack(),
                ProviderLayers.GetProviderInterfaceIndex(typeof(IBaseProvider)));
            Assert.AreEqual(1, provider.Count);
            Assert.IsTrue(provider.Contains(typeof(TestProvider)));
        }

        [TestMethod]
        public void SecondaryInterface()
        {
            var ruleProvider = Discovery.GetImplementationType(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetImplementationType(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetImplementationType(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.AreEqual(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetImplementationType(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [TestMethod]
        public void HasSecondaryInterface()
        {
            var ruleProvider = Discovery.HasImplementationType(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasImplementationType(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.IsTrue(cacheProvider);

            var compressionProvider = Discovery.HasImplementationType(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasImplementationType(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.IsTrue(dualbaseProvider);
        }

        [TestMethod]
        public void ManySecondaryInterface()
        {
            var providers = Discovery.GetImplementationTypes(typeof(ITestProvider), typeof(IBaseProvider));
            Assert.AreEqual(5, providers.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCacheProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCompressionProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetImplementationTypes(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.AreEqual(1, ruleProviders.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
        }

        [TestMethod]
        public void SingleInterface()
        {
            var ruleProvider = Discovery.GetImplementationType(typeof(IRuleProvider));
            Assert.AreEqual(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetImplementationType(typeof(ICacheProvider));
            Assert.AreEqual(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetImplementationType(typeof(ICompressionProvider));
            Assert.AreEqual(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetImplementationType(typeof(IDualBaseProvider));
            Assert.AreEqual(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [TestMethod]
        public void HasSingleInterface()
        {
            var ruleProvider = Discovery.HasImplementationType(typeof(IRuleProvider));
            Assert.IsTrue(ruleProvider);

            var cacheProvider = Discovery.HasImplementationType(typeof(ICacheProvider));
            Assert.IsTrue(cacheProvider);

            var compressionProvider = Discovery.HasImplementationType(typeof(ICompressionProvider));
            Assert.IsTrue(compressionProvider);

            var dualbaseProvider = Discovery.HasImplementationType(typeof(IDualBaseProvider));
            Assert.IsTrue(dualbaseProvider);
        }

        [TestMethod]
        public void ManySingleInterface()
        {
            var providers = Discovery.GetImplementationTypes(typeof(IBaseProvider));
            Assert.AreEqual(7, providers.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCacheProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestCompressionProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.IsTrue(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetImplementationTypes(typeof(IRuleProvider));
            Assert.AreEqual(1, ruleProviders.Count);
            Assert.IsTrue(providers.Contains(typeof(TestRuleProvider)));
        }
    }
}
