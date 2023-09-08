using System;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
using Zerra.CQRS.RabbitMQ;
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

            Bus.AddLogger(new BusLoggingProvider());

            var serviceSettings = CQRSSettings.Get(settingsName);

            //Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp for backend only services
            IServiceCreator serviceCreator = new TcpServiceCreator();

            //Option1B: Enable this for Http which can be access directly from a front end
            //var authorizer = new DemoCookieApiAuthorizer();
            //var serviceCreator = new HttpServiceCreator(authorizer, null);

            //Option1C: Enable this using RabbitMQ for event streaming commands/events
            serviceCreator = new RabbitMQServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option1D: Enable this using Kafka for event streaming commands/events
            //serviceCreator = new KafkaServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option1E: Enable this using Azure Event Hub for event streaming commands/events
            //serviceCreator = new AzureEventHubServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option1F: Enable this using Azure Service Bus for event streaming commands/events
            //serviceCreator = new AzureServiceBusServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Enable one of the following routing options
            //----------------------------------------------------------

            //Option2A: Enable this for direct service communication, no custom relay/loadbalancer (can still use container balancers)
            Bus.StartServices(serviceSettings, serviceCreator);

            //Option2B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
        }
    }
}
