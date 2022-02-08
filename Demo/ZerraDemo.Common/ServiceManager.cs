using Zerra.CQRS;
using Zerra.CQRS.Kafka;
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

            var serviceCreatorInternal = new TcpInternalServiceCreator();



            //Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp for backend only services
            var serviceCreator = serviceCreatorInternal;

            //Option1B: Enable this for Http which can be access directly from a front end
            //var authorizor = new DemoCookieApiAuthorizer();
            //var serviceCreator = new TcpApiServiceCreator(authorizor, null);

            //Option1C: Enable this using RabbitMQ for event streaming commands/events
            //var serviceCreator = new RabbitMQServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);

            //Option1D: Enable this using Kafka for event streaming commands/events
            //var serviceCreator = new KafkaServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);



            //Enable one of the following routing options
            //----------------------------------------------------------

            //Option2A: Enable this for direct service communication, no custom relay/loadbalancer (can still use container balancers)
            Bus.StartServices(settingsName, serviceSettings, serviceCreator, null);

            //Option2B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
        }
    }
}
