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
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger1.ILedger1QueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger1.Ledger1QueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.Ledger1.EventStore.Ledger1EventStoreDataContext).Assembly.ToString());

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
