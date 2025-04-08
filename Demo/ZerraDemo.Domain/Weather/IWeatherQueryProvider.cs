using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS;
using ZerraDemo.Domain.Weather.Models;

namespace ZerraDemo.Domain.Weather
{
    [ServiceExposed]
    public interface IWeatherQueryProvider
    {
        Task<WeatherModel> GetWeather();
        WeatherModel GetWeatherSync(CancellationToken cancellationToken);
        Task<Stream> TestStreams();
    }
}
