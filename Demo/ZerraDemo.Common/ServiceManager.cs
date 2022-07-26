using System;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.Settings;
using Zerra.Logger;

namespace ZerraDemo.Common
{
    public static class ServiceManager
    {
        public static void StartServices()
        {
            var settingsName = Config.EntryAssemblyName;
            if (settingsName == null)
                throw new Exception($"Entry Assembly is null, {nameof(ServiceManager)} cannot identify which service is running");

            Bus.AddMessageLogger(new MessageLoggingProvider());

            var serviceSettings = CQRSSettings.Get(settingsName);

            //Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp for backend only services
            var serviceCreator = new TcpServiceCreator();

            //Option1B: Enable this for Http which can be access directly from a front end
            //var authorizor = new DemoCookieApiAuthorizer();
            //var serviceCreator = new HttpServiceCreator(authorizor, null);

            //Option1C: Enable this using RabbitMQ for event streaming commands/events
            //var serviceCreator = new RabbitMQServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);

            //Option1D: Enable this using Kafka for event streaming commands/events
            //var serviceCreator = new KafkaServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);

            //Option1E: Enable this using Azure Event Hub for event streaming commands/events
            //var serviceCreator = new AzureEventHubServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);

            //Option1F: Enable this using Azure Service Bus for event streaming commands/events
            //var serviceCreator = new AzureServiceBusServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal, Config.EnvironmentName);

            //Enable one of the following routing options
            //----------------------------------------------------------

            //Option2A: Enable this for direct service communication, no custom relay/loadbalancer (can still use container balancers)
            Bus.StartServices(settingsName, serviceSettings, serviceCreator);

            //Option2B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
        }
    }
}
