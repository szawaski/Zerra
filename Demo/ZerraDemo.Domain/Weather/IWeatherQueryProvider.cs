using System.IO;
using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.Weather.Models;

namespace ZerraDemo.Domain.Weather
{
    [ServiceExposed]
    public interface IWeatherQueryProvider
    {
        Task<WeatherModel> GetWeather();
        Task<Stream> TestStreams();
    }
}
