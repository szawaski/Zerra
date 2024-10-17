using System;
using System.Reflection;
using Zerra.CQRS;

namespace Zerra.Reflection
{
    public static class SourceGenerationRegistration
    {
        public static void MarkAssemblyAsDiscovered(Assembly assembly) => Discovery.MarkAssemblyAsDiscovered(assembly);
        public static void DiscoverType(Type typeInAssembly, Type[] interfaceTypes) => Discovery.DiscoverType(typeInAssembly, interfaceTypes, false);
        public static void RegisterEmptyImplementation(Type interfaceType, Type type) => EmptyImplementations.RegisterEmptyImplementation(interfaceType, type);
        public static void RegisterCaller(Type interfaceType, Type type) => BusRouters.RegisterCaller(interfaceType, type);
    }
}
