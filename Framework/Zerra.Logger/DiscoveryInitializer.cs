//Zerra Generated File
#if NET5_0_OR_GREATER

namespace Zerra.Logger
{
    internal static class DiscoveryInitializer
    {
#pragma warning disable CA2255
        [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            Zerra.Reflection.Discovery.DiscoverType(typeof(Zerra.Logger.LoggingProvider));
        }
    }
}

#endif
