using System.IO;
using System.Threading.Tasks;
using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Models;

namespace ZerraDemo.Domain.WeatherCached
{
    [ServiceExposed]
    public interface IWeatherCachedQueryProvider
    {
        Task<WeatherCachedModel> GetWeather();
        Task<Stream> TestStreams();
    }
}
