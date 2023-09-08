using Zerra.CQRS;
using Zerra.Providers;
using ZerraDemo.Domain.Weather.Commands;

namespace ZerraDemo.Domain.Weather
{
    public interface IWeatherCommandHandler :
        ICommandHandler<SetWeatherCommand>
    {
    }
}
