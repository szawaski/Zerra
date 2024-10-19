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
                Console.WriteLine(typeof(ZerraDemo.Domain.Pets.IPetsQueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Pets.PetsQueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Pets.Sql.PetsDataContext).Assembly.ToString());

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
