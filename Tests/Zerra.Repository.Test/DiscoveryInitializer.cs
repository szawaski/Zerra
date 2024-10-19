//Zerra Generated File
#if NET5_0_OR_GREATER

namespace Zerra.Repository.Test
{
    internal static class DiscoveryInitializer
    {
#pragma warning disable CA2255
        [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MsSqlTestSqlDataContext), []);
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MySqlTestSqlDataContext), []);
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.PostgreSqlTestSqlDataContext), []);

            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MsSqlTestRelationsModel), []);
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MsSqlTestTypesModel), []);

            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MySqlTestRelationsModel), []);
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.MySqlTestTypesModel), []);

            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.PostgreSqlTestRelationsModel), []);
            Zerra.Reflection.SourceGenerationRegistration.DiscoverType(typeof(Zerra.Repository.Test.PostgreSqlTestTypesModel), []);

            Zerra.Reflection.SourceGenerationRegistration.RunGenerationsFromAttributes();
        }
    }
}

#endif
