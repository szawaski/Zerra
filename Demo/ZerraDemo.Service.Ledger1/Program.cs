using Zerra;
using Zerra.CQRS;
using ZerraDemo.Common;

namespace ZerraDemo.Service.Ledger
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.LoadConfiguration(args);
            ServiceManager.StartServices();
            Bus.WaitForProcessExit();
        }
    }
}
