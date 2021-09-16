using System.Threading.Tasks;

namespace Zerra.CQRS.Relay
{
    public interface IRelayRegister
    {
        string RelayUrl { get; }
        Task Register(string serviceUrl, string[] providerTypes);
        Task Unregister(string serviceUrl);
    }
}
