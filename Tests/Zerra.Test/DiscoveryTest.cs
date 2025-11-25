// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
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

    internal static class DiscoveryInitializer
    {
#pragma warning disable CA2255
        [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestRuleProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestCacheProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestCompressionProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestDualBaseProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestBusCacheProvider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestDuplicate1Provider));
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Test.TestDuplicate2Provider));
        }
    }

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

        [Fact]
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

        [Fact]
        public void Duplicates()
        {
            var topLayerProvider = Discovery.GetClassByInterface(typeof(ITestDuplicateProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface, false);
            Assert.Null(topLayerProvider);
        }

        [Fact]
        public void Layers()
        {
            var topLayerProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.Equal(typeof(TestRuleProvider), topLayerProvider);

            var ruleProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface, false);
            Assert.Equal(typeof(TestDualBaseProvider), compressionProvider); //Compression Provider has IIgnoreProviderResolver

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(typeof(TestDualBaseProvider), dualbaseProvider);

            var provider = Discovery.GetClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.Equal(typeof(TestProvider), provider);
        }

        [Fact]
        public void HasLayers()
        {
            var topLayerProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.True(topLayerProvider);

            var ruleProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.True(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.True(cacheProvider);

            var encryptionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IEncryptionProvider)), ProviderResolver.IgnoreInterface);
            Assert.True(encryptionProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface);
            Assert.True(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.True(dualbaseProvider);

            var provider = Discovery.HasClassByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.True(provider);
        }

        [Fact]
        public void ManyLayers()
        {
            var topLayerProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack, 0, ProviderResolver.IgnoreInterface);
            Assert.Equal(1, topLayerProvider.Count);
            Assert.True(topLayerProvider.Contains(typeof(TestRuleProvider)));

            var ruleProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IRuleProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(1, ruleProvider.Count);
            Assert.True(ruleProvider.Contains(typeof(TestRuleProvider)));

            var cacheProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICacheProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(1, cacheProvider.Count);
            Assert.True(cacheProvider.Contains(typeof(TestCacheProvider)));

            var compressionProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(ICompressionProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(1, compressionProvider.Count);  //Compression Provider has IIgnoreProviderResolver
            Assert.True(compressionProvider.Contains(typeof(TestDualBaseProvider))); //Compression Provider has IIgnoreProviderResolver

            var dualbaseProvider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(typeof(IDualBaseProvider)), ProviderResolver.IgnoreInterface);
            Assert.Equal(1, dualbaseProvider.Count);
            Assert.True(dualbaseProvider.Contains(typeof(TestDualBaseProvider)));

            var provider = Discovery.GetClassesByInterface(typeof(ITestProvider), ProviderResolver.InterfaceStack,
                GetInterfaceIndex(null), ProviderResolver.IgnoreInterface);
            Assert.Equal(1, provider.Count);
            Assert.True(provider.Contains(typeof(TestProvider)));
        }

        [Fact]
        public void SecondaryInterface()
        {
            var ruleProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.Equal(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.Equal(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.Equal(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.Equal(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [Fact]
        public void HasSecondaryInterface()
        {
            var ruleProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.True(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(ICacheProvider));
            Assert.True(cacheProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(ICompressionProvider));
            Assert.True(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(ITestProvider), typeof(IDualBaseProvider));
            Assert.True(dualbaseProvider);
        }

        [Fact]
        public void ManySecondaryInterface()
        {
            var providers = Discovery.GetClassesByInterface(typeof(ITestProvider));
            Assert.Equal(6, providers.Count);
            Assert.True(providers.Contains(typeof(TestRuleProvider)));
            Assert.True(providers.Contains(typeof(TestCacheProvider)));
            Assert.True(providers.Contains(typeof(TestCompressionProvider)));
            Assert.True(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.True(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetClassesByInterface(typeof(ITestProvider), typeof(IRuleProvider));
            Assert.Equal(1, ruleProviders.Count);
            Assert.True(providers.Contains(typeof(TestRuleProvider)));
        }

        [Fact]
        public void SingleInterface()
        {
            var ruleProvider = Discovery.GetClassByInterface(typeof(IRuleProvider));
            Assert.Equal(typeof(TestRuleProvider), ruleProvider);

            var cacheProvider = Discovery.GetClassByInterface(typeof(ICacheProvider));
            Assert.Equal(typeof(TestCacheProvider), cacheProvider);

            var compressionProvider = Discovery.GetClassByInterface(typeof(ICompressionProvider));
            Assert.Equal(typeof(TestCompressionProvider), compressionProvider);

            var dualbaseProvider = Discovery.GetClassByInterface(typeof(IDualBaseProvider));
            Assert.Equal(typeof(TestDualBaseProvider), dualbaseProvider);
        }

        [Fact]
        public void HasSingleInterface()
        {
            var ruleProvider = Discovery.HasClassByInterface(typeof(IRuleProvider));
            Assert.True(ruleProvider);

            var cacheProvider = Discovery.HasClassByInterface(typeof(ICacheProvider));
            Assert.True(cacheProvider);

            var compressionProvider = Discovery.HasClassByInterface(typeof(ICompressionProvider));
            Assert.True(compressionProvider);

            var dualbaseProvider = Discovery.HasClassByInterface(typeof(IDualBaseProvider));
            Assert.True(dualbaseProvider);
        }

        [Fact]
        public void ManySingleInterface()
        {
            var providers = Discovery.GetClassesByInterface(typeof(ITestProvider));
            Assert.Equal(6, providers.Count);
            Assert.True(providers.Contains(typeof(TestRuleProvider)));
            Assert.True(providers.Contains(typeof(TestCacheProvider)));
            Assert.True(providers.Contains(typeof(TestCompressionProvider)));
            Assert.True(providers.Contains(typeof(TestDualBaseProvider)));
            Assert.True(providers.Contains(typeof(TestProvider)));

            var ruleProviders = Discovery.GetClassesByInterface(typeof(IRuleProvider));
            Assert.Equal(1, ruleProviders.Count);
            Assert.True(providers.Contains(typeof(TestRuleProvider)));
        }
    }
}
