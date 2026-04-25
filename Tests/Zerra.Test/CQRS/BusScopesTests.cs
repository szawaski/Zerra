// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;

namespace Zerra.Test.CQRS
{
    public class BusScopesTests
    {
        private interface ITestDependency { }
        private class TestDependency : ITestDependency { }

        [Fact]
        public void Constructor_CreatesEmptyDependencies()
        {
            var busScopes = new BusServices();

            Assert.NotNull(busScopes);
            Assert.Empty(busScopes.Dependencies);
        }

        [Fact]
        public void AddService_WithInterface()
        {
            var busScopes = new BusServices();
            var dependency = new TestDependency();

            busScopes.AddService<ITestDependency>(dependency);

            Assert.Single(busScopes.Dependencies);
            Assert.True(busScopes.Dependencies.ContainsKey(typeof(ITestDependency)));
            Assert.Same(dependency, busScopes.Dependencies[typeof(ITestDependency)]);
        }

        [Fact]
        public void AddService_WithNullInstance_ThrowsArgumentNullException()
        {
            var busScopes = new BusServices();

            Assert.Throws<ArgumentNullException>(() => busScopes.AddService<ITestDependency>(null!));
        }

        [Fact]
        public void AddService_WithNonInterfaceType_ThrowsArgumentException()
        {
            var busScopes = new BusServices();

            Assert.Throws<ArgumentException>(() => busScopes.AddService<TestDependency>(new TestDependency()));
        }

        [Fact]
        public void AddService_MultipleDependencies()
        {
            var busScopes = new BusServices();
            var dep1 = new TestDependency();

            busScopes.AddService<ITestDependency>(dep1);

            Assert.Single(busScopes.Dependencies);
        }

        [Fact]
        public void AddService_OverwriteExisting()
        {
            var busScopes = new BusServices();
            var dep1 = new TestDependency();
            var dep2 = new TestDependency();

            busScopes.AddService<ITestDependency>(dep1);
            busScopes.AddService<ITestDependency>(dep2);

            Assert.Single(busScopes.Dependencies);
            Assert.Same(dep2, busScopes.Dependencies[typeof(ITestDependency)]);
        }

        [Fact]
        public void AddService_StoresCorrectType()
        {
            var busScopes = new BusServices();
            var dependency = new TestDependency();

            busScopes.AddService<ITestDependency>(dependency);

            Assert.True(busScopes.Dependencies.ContainsKey(typeof(ITestDependency)));
            Assert.False(busScopes.Dependencies.ContainsKey(typeof(TestDependency)));
        }

        [Fact]
        public void Dependencies_IsAccessible()
        {
            var busScopes = new BusServices();

            var dependencies = busScopes.Dependencies;

            Assert.NotNull(dependencies);
            Assert.IsType<Dictionary<Type, object>>(dependencies);
        }
    }
}
