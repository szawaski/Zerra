using System;
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
                Console.WriteLine(typeof(ZerraDemo.Domain.WeatherCached.IWeatherCachedQueryProvider).Assembly.ToString());
                Console.WriteLine(typeof(ZerraDemo.Domain.WeatherCached.WeatherCachedQueryProvider).Assembly.ToString());

                Config.AssemblyLoaderEnabled = false;

                Config.LoadConfiguration(args);
            });
            Bus.WaitForExit();
        }
    }
}
