using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.WeatherCached.Commands;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandlerCache : BaseLayerProvider<IWeatherCachedCommandHandler>, IWeatherCachedCommandHandler, IBusCache
    {
        public async Task Handle(SetWeatherCachedCommand command)
        {
            WeatherCachedServerCache.Model = null;
            await NextProvider.Handle(command);
        }
    }
}
