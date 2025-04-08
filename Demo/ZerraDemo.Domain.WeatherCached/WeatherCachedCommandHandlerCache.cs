using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Commands;
using System.Threading;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandlerCache : LayerProvider<IWeatherCachedCommandHandler>, IWeatherCachedCommandHandler, IBusCache
    {
        public async Task Handle(SetWeatherCachedCommand command, CancellationToken cancellationToken)
        {
            await NextProvider.Handle(command, cancellationToken);
            WeatherCachedServerCache.Model = null;
        }
    }
}
