using System.IO;
using System.Threading.Tasks;
using ZerraDemo.Domain.WeatherCached.Models;
using Zerra.Providers;
using Zerra.CQRS;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedQueryProviderCache : LayerProvider<IWeatherCachedQueryProvider>, IWeatherCachedQueryProvider, IBusCache
    {
        public async Task<WeatherCachedModel> GetWeather()
        {
            WeatherCachedServerCache.Model ??= await NextProvider.GetWeather();
            return WeatherCachedServerCache.Model;
        }

        public Task<Stream> TestStreams()
        {
            return NextProvider.TestStreams();
        }
    }
}
