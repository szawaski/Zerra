using System.Threading.Tasks;
using ZerraDemo.Domain.WeatherCached.Constants;

namespace ZerraDemo.Domain.WeatherCached
{
    public static class WeatherCachedServer
    {
        private static WeatherCachedType weatherType = WeatherCachedType.Rain;
        public static async Task SetWeather(WeatherCachedType weatherType)
        {
            await Task.Delay(1); //processing
            WeatherCachedServer.weatherType = weatherType;
        }
        public static async Task<WeatherCachedType> GetWeather()
        {
            await Task.Delay(1); //calculating
            return weatherType;
        }
    }
}
