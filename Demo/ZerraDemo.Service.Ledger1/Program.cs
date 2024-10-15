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
                var assemblyLoader1 = typeof(ZerraDemo.Domain.Ledger1.ILedger1QueryProvider);
                var assemblyLoader2 = typeof(ZerraDemo.Domain.Ledger1.Ledger1QueryProvider);
                var assemblyLoader3 = typeof(ZerraDemo.Domain.Ledger1.EventStore.Ledger1EventStoreDataContext);

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
