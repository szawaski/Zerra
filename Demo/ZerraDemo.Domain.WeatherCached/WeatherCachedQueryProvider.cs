using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZerraDemo.Domain.WeatherCached.Models;
using ZerraDemo.Common;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedQueryProvider : IWeatherCachedQueryProvider
    {
        public async Task<WeatherCachedModel> GetWeather()
        {
            Access.CheckRole("Admin");

            var weatherType = await WeatherCachedServer.GetWeather();
            return new WeatherCachedModel
            {
                Date = DateTime.UtcNow,
                WeatherType = weatherType
            };
        }

        public Task<Stream> TestStreams()
        {
            Access.CheckRole("Admin");

            var sb = new StringBuilder();
            for (var i = 0; i < 100000; i++)
                _ = sb.Append("0123456789");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            return Task.FromResult<Stream>(stream);
        }
    }
}
