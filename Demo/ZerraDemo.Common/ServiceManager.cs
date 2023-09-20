using System;
using Zerra;
using Zerra.CQRS;
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

            var serviceSettings = CQRSSettings.Get(settingsName, false);

            IServiceCreator serviceCreator;

            //Option1: Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp (backend only services)
            serviceCreator = new TcpServiceCreator();

            //Option1B: Enable this for Http
            //var authorizer = new DemoCookieApiAuthorizer();
            //serviceCreator = new HttpServiceCreator(authorizer, null);

            //Option1C: Enable this using Kestrel Http (Required for Azure Apps)
            //var kestrelServiceCreator = new KestrelServiceCreator(app, null, ContentType.Bytes);

            //Option2: Enable one of the following message services
            //----------------------------------------------------------

            //Option2A: Enable this using RabbitMQ for commands/events
            serviceCreator = new RabbitMQServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option2B: Enable this using Kafka for commands/events
            //serviceCreator = new KafkaServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option2C: Enable this using Azure Event Hub for commands/events
            //serviceCreator = new AzureEventHubServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option2D: Enable this using Azure Service Bus for commands/events
            //serviceCreator = new AzureServiceBusServiceCreator(serviceSettings.MessageHost, serviceCreator, Config.EnvironmentName);

            //Option3: Enable one of the following routing options
            //----------------------------------------------------------

            //Option3A: Enable this for direct service communication, no custom relay/loadbalancer (can still use container balancers)
            Bus.StartServices(serviceSettings, serviceCreator);

            //Option3B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
        }
    }
}
