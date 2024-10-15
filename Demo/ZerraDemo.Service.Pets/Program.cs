using System;
using Zerra;
using Zerra.CQRS;
using ZerraDemo.Common;

namespace ZerraDemo.Service.Pets
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceManager.StartServices(() =>
            {
                var assemblyLoader1 = typeof(ZerraDemo.Domain.Pets.IPetsQueryProvider);
                var assemblyLoader2 = typeof(ZerraDemo.Domain.Pets.PetsQueryProvider);
                var assemblyLoader3 = typeof(ZerraDemo.Domain.Pets.Sql.PetsDataContext);

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
