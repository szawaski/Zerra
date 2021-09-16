using System.Threading.Tasks;
using ZerraDemo.Domain.Weather.Constants;

namespace ZerraDemo.Domain.Weather
{
    public static class WeatherServer
    {
        private static WeatherType weatherType = WeatherType.Rain;
        public static async Task SetWeather(WeatherType weatherType)
        {
            await Task.Delay(1); //processing
            WeatherServer.weatherType = weatherType;
        }
        public static async Task<WeatherType> GetWeather()
        {
            await Task.Delay(1); //calculating
            return weatherType;
        }
    }
}
