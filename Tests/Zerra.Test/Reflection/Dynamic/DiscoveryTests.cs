// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using Zerra.Map;
using Zerra.Reflection.Dynamic;

namespace Zerra.Test.Reflection.Dynamic
{
    public class DiscoveryTests
    {
        //discovery skips Zerra's own assemblies, this one included, so the types it finds are built into an assembly of their own
        private static readonly Assembly discoveredAssembly = BuildAssembly();

        private static Assembly BuildAssembly()
        {
            var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName("DiscoveryTestTypes"), typeof(object).Assembly);
            var module = assemblyBuilder.DefineDynamicModule("DiscoveryTestTypes");

            var service = module.DefineType("DiscoveryTestTypes.IService", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            var unused = module.DefineType("DiscoveryTestTypes.IUnused", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            var single = module.DefineType("DiscoveryTestTypes.ISingle", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            var generic = module.DefineType("DiscoveryTestTypes.IGeneric`1", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            _ = generic.DefineGenericParameters("TValue");

            var serviceBase = module.DefineType("DiscoveryTestTypes.ServiceBase", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Class, typeof(object));
            serviceBase.AddInterfaceImplementation(service);
            var genericBase = module.DefineType("DiscoveryTestTypes.GenericBase`1", TypeAttributes.Public | TypeAttributes.Class, typeof(object));
            _ = genericBase.DefineGenericParameters("TItem");

            var serviceA = module.DefineType("DiscoveryTestTypes.ServiceA", TypeAttributes.Public | TypeAttributes.Class, serviceBase);
            serviceA.AddInterfaceImplementation(service);
            serviceA.AddInterfaceImplementation(single);
            serviceA.SetCustomAttribute(new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes)!, []));
            var serviceB = module.DefineType("DiscoveryTestTypes.ServiceB", TypeAttributes.Public | TypeAttributes.Class, serviceBase);
            serviceB.AddInterfaceImplementation(service);

            var genericImplementation = module.DefineType("DiscoveryTestTypes.GenericImplementation", TypeAttributes.Public | TypeAttributes.Class, typeof(object));
            genericImplementation.AddInterfaceImplementation(generic.MakeGenericType(typeof(int)));
            var closedDerived = module.DefineType("DiscoveryTestTypes.ClosedDerived", TypeAttributes.Public | TypeAttributes.Class, genericBase.MakeGenericType(typeof(int)));
            var openDerived = module.DefineType("DiscoveryTestTypes.OpenDerived`1", TypeAttributes.Public | TypeAttributes.Class);
            var openDerivedParameters = openDerived.DefineGenericParameters("TOther");
            openDerived.SetParent(genericBase.MakeGenericType(openDerivedParameters[0]));

            var discoveredMap = module.DefineType("DiscoveryTestTypes.DiscoveredMap", TypeAttributes.Public | TypeAttributes.Class, typeof(object));
            discoveredMap.AddInterfaceImplementation(typeof(IMapDefinition<DiscoveredMapSource, DiscoveredMapTarget>));
            _ = discoveredMap.DefineDefaultConstructor(MethodAttributes.Public);
            var define = discoveredMap.DefineMethod("Define", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot, typeof(void), [typeof(IMapSetup<DiscoveredMapSource, DiscoveredMapTarget>)]);
            var il = define.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(DiscoveredMapSource).GetMethod(nameof(DiscoveredMapSource.Define))!);
            il.Emit(OpCodes.Ret);

            var openMap = module.DefineType("DiscoveryTestTypes.OpenMap`1", TypeAttributes.Public | TypeAttributes.Class, typeof(object));
            var openMapParameters = openMap.DefineGenericParameters("TItem");
            openMap.AddInterfaceImplementation(typeof(IMapDefinition<,>).MakeGenericType(openMapParameters[0], openMapParameters[0]));
            var openDefine = openMap.DefineMethod("Define", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot, typeof(void), [typeof(IMapSetup<,>).MakeGenericType(openMapParameters[0], openMapParameters[0])]);
            openDefine.GetILGenerator().Emit(OpCodes.Ret);

            foreach (var type in new[] { service, unused, single, generic, serviceBase, genericBase, serviceA, serviceB, genericImplementation, closedDerived, openDerived, discoveredMap, openMap })
                _ = type.CreateType();

            var path = Path.Combine(Path.GetTempPath(), $"DiscoveryTestTypes_{Guid.NewGuid():N}.dll");
            assemblyBuilder.Save(path);
            return Assembly.LoadFrom(path);
        }

        private static Type Get(string name) => discoveredAssembly.GetType($"DiscoveryTestTypes.{name}", true)!;

        [Fact]
        public void Lookups()
        {
            var service = Get("IService");
            var single = Get("ISingle");
            var unused = Get("IUnused");
            var genericOpen = Get("IGeneric`1");
            var serviceBase = Get("ServiceBase");
            var genericBaseOpen = Get("GenericBase`1");
            var serviceA = Get("ServiceA");
            var serviceB = Get("ServiceB");
            var genericImplementation = Get("GenericImplementation");
            var closedDerived = Get("ClosedDerived");
            var openDerived = Get("OpenDerived`1");

            Discovery.Initialize(true);
            Discovery.Initialize(false);


            //interfaces, the abstract base is a type with the interface but not a class
            Assert.True(Discovery.HasTypeByInterface(service));
            Assert.True(Discovery.HasClassByInterface(service));
            Assert.False(Discovery.HasTypeByInterface(unused));
            Assert.False(Discovery.HasClassByInterface(unused));
            Assert.Equal([serviceA, serviceB, serviceBase], Discovery.GetTypesByInterface(service).OrderBy(x => x.Name));
            Assert.Equal([serviceA, serviceB], Discovery.GetClassesByInterface(service).OrderBy(x => x.Name));
            Assert.Empty(Discovery.GetTypesByInterface(unused));
            Assert.Empty(Discovery.GetClassesByInterface(unused));
            Assert.Equal(serviceA, Discovery.GetClassByInterface(single));
            Assert.Equal(serviceA, Discovery.GetTypeByInterface(single));
            Assert.Null(Discovery.GetClassByInterface(service, false));
            Assert.Null(Discovery.GetTypeByInterface(service, false));
            Assert.Null(Discovery.GetClassByInterface(unused, false));
            Assert.Null(Discovery.GetTypeByInterface(unused, false));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetClassByInterface(service));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetTypeByInterface(service));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetClassByInterface(unused));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetTypeByInterface(unused));
            Assert.Contains(service, Discovery.GetInterfacesByType(serviceA));
            Assert.Contains(single, Discovery.GetInterfacesByType(serviceA));
            Assert.Empty(Discovery.GetInterfacesByType(typeof(DiscoveryTests)));

            //an open generic interface finds what implements any of its closed forms
            Assert.True(Discovery.HasTypeByInterface(genericOpen));
            Assert.True(Discovery.HasClassByInterface(genericOpen));
            Assert.Equal(genericImplementation, Discovery.GetClassByInterface(genericOpen));
            Assert.Equal(genericImplementation, Discovery.GetTypeByInterface(genericOpen));
            Assert.Equal([genericImplementation], Discovery.GetClassesByInterface(genericOpen));
            Assert.Equal([genericImplementation], Discovery.GetTypesByInterface(genericOpen));
            Assert.Equal(genericImplementation, Discovery.GetClassByInterface(genericOpen.MakeGenericType(typeof(int))));

            //base types, an open generic base finds classes deriving from its closed and open forms
            Assert.True(Discovery.HasClassByBaseType(serviceBase));
            Assert.Equal([serviceA, serviceB], Discovery.GetClassesByBaseType(serviceBase).OrderBy(x => x.Name));
            Assert.Null(Discovery.GetClassByBaseType(serviceBase, false));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetClassByBaseType(serviceBase));
            Assert.True(Discovery.HasClassByBaseType(genericBaseOpen));
            Assert.Equal([closedDerived, openDerived], Discovery.GetClassesByBaseType(genericBaseOpen).OrderBy(x => x.Name));
            Assert.Equal(closedDerived, Discovery.GetClassByBaseType(genericBaseOpen.MakeGenericType(typeof(int))));
            Assert.False(Discovery.HasClassByBaseType(typeof(DiscoveryTests)));
            Assert.Empty(Discovery.GetClassesByBaseType(typeof(DiscoveryTests)));
            Assert.Null(Discovery.GetClassByBaseType(typeof(DiscoveryTests), false));
            _ = Assert.ThrowsAny<Exception>(() => Discovery.GetClassByBaseType(typeof(DiscoveryTests)));

            //attributes
            Assert.Contains(serviceA, Discovery.GetTypesFromAttribute(typeof(ObsoleteAttribute)));
            Assert.Empty(Discovery.GetTypesFromAttribute(typeof(DiscoveryTestsAttribute)));

            //defining a class takes the place of the ones found
            Discovery.DefineClassByInterface(service, serviceB);
            Assert.Equal(serviceB, Discovery.GetClassByInterface(service));
            _ = Assert.Throws<ArgumentException>(() => Discovery.DefineClassByInterface(single, serviceB));
            _ = Assert.Throws<ArgumentException>(() => Discovery.DefineClassByInterface(service, serviceBase));
            _ = Assert.Throws<ArgumentException>(() => Discovery.DefineClassByInterface(serviceBase, serviceA));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.DefineClassByInterface(null!, serviceA));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.DefineClassByInterface(service, null!));
            _ = Assert.Throws<ArgumentException>(() => Discovery.DefineClassByInterface<IDisposable>(typeof(DiscoveryTests)));

            //arguments
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.HasTypeByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.HasClassByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.HasClassByBaseType(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetTypeByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetClassByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetClassByBaseType(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetTypesByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetClassesByInterface(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetClassesByBaseType(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetInterfacesByType(null!));
            _ = Assert.Throws<ArgumentNullException>(() => Discovery.GetTypesFromAttribute(null!));
            _ = Assert.Throws<ArgumentException>(() => Discovery.HasTypeByInterface(serviceA));
            _ = Assert.Throws<ArgumentException>(() => Discovery.HasClassByInterface(serviceA));
            _ = Assert.Throws<ArgumentException>(() => Discovery.GetTypeByInterface(serviceA));
            _ = Assert.Throws<ArgumentException>(() => Discovery.GetClassByInterface(serviceA));
            _ = Assert.Throws<ArgumentException>(() => Discovery.GetTypesByInterface(serviceA));
            _ = Assert.Throws<ArgumentException>(() => Discovery.GetClassesByInterface(serviceA));
            //maps found by discovery are registered
            MapDiscovery.Initialize();
            Assert.Equal("discovered 7", new DiscoveredMapSource() { Value = 7 }.Map<DiscoveredMapSource, DiscoveredMapTarget>().Text);
        }

        public sealed class DiscoveredMapSource
        {
            public int Value { get; set; }
            public static void Define(IMapSetup<DiscoveredMapSource, DiscoveredMapTarget> map) => map.Define(x => x.Text, x => $"discovered {x.Value}");
        }
        public sealed class DiscoveredMapTarget { public string? Text { get; set; } }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DiscoveryTestsAttribute : Attribute { }
}
