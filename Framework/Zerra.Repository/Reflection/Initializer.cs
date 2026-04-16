using System.Reflection;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Repository.Reflection
{
    internal static class Initializer
    {
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        public static void Initialize()
        {
            Discovery.Initialize(true);
        }
    }
}
