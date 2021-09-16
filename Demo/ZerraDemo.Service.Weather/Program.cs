using Zerra;
using Zerra.CQRS;
using ZerraDemo.Common;

namespace ZerraDemo.Service.Weather
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.LoadConfiguration(args);
            ServiceManager.StartServices();
            Bus.WaitUntilExit();
        }
    }
}
