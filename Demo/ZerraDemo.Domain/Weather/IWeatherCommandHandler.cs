using Zerra.CQRS;
using ZerraDemo.Domain.Weather.Commands;

namespace ZerraDemo.Domain.Weather
{
    public interface IWeatherCommandHandler :
        ICommandHandler<SetWeatherCommand>
    {
    }
}
