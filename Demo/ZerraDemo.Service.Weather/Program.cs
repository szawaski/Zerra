using Zerra;
using Zerra.CQRS;
using ZerraDemo.Common;

namespace ZerraDemo.Service.Weather
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceManager.StartServices(() =>
            {
                var assemblyLoader1 = typeof(ZerraDemo.Domain.Weather.IWeatherQueryProvider);
                var assemblyLoader2 = typeof(ZerraDemo.Domain.Weather.WeatherQueryProvider);

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
