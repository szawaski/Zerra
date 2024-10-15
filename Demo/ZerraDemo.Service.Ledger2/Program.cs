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
                var assemblyLoader1 = typeof(ZerraDemo.Domain.Ledger2.ILedger2QueryProvider);
                var assemblyLoader2 = typeof(ZerraDemo.Domain.Ledger2.Ledger2QueryProvider);
                var assemblyLoader3 = typeof(ZerraDemo.Domain.Ledger2.EventStore.Ledger2EventStoreDataContext);

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
