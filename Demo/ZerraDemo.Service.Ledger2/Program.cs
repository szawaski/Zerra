using System;
using Zerra;
using Zerra.CQRS;
using ZerraDemo.Common;

namespace ZerraDemo.Service.Ledger
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceManager.StartServices(() =>
            {
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger2.ILedger2QueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger2.Ledger2QueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger2.EventStore.Ledger2EventStoreDataContext).Assembly.ToString());

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
