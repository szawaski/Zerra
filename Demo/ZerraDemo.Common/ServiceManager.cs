using Zerra.CQRS;
using Zerra.CQRS.Settings;
using Zerra.Logger;

namespace ZerraDemo.Common
{
    public static class ServiceManager
    {
        public static void StartServices()
        {
            var settingsName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

            var serviceSettings = CQRSSettings.Get();

            Bus.AddMessageLogger(new MessageLoggingProvider());

            //Option1A: Enable this for Tcp for backend only services
            var serviceCreator = new TcpInternalServiceCreator();

            //Option1B: Enable this for Http which can be access directly from a front end
            //var authorizor = new DemoCookieApiAuthorizer();
            //var serviceCreator = new TcpApiServiceCreator(authorizor, null);

            //Option1C: Enable this using RabbitMQ for event streaming commands/events
            //var serviceCreator = new RabbitServiceCreator(serviceSettings.MessageHost);

            //Option2A: Enable this for direct service communication, no relay/loadbalancer
            Bus.StartServices(settingsName, serviceSettings, serviceCreator, null);

            //Option2B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
        }
    }
}
