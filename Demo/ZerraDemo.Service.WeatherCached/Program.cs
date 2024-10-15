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
                var assemblyLoader1 = typeof(ZerraDemo.Domain.WeatherCached.IWeatherCachedQueryProvider);
                var assemblyLoader2 = typeof(ZerraDemo.Domain.WeatherCached.WeatherCachedQueryProvider);

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
