using System.Diagnostics;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.CQRS.Settings;
using Zerra.Logging;
using Zerra.Logger;

namespace ZerraDemo.Common
{
    public static class ServiceManager
    {
        public static void StartServices(int? receiveCommandsBeforeExit = null)
        {
            var timer = Stopwatch.StartNew();
            Bus.ReceiveCommandsBeforeExit = receiveCommandsBeforeExit;

            Bus.AddLogger(new BusLoggingProvider());

            //bindingUrlFromStandardVariables = false means that
            //environmental varibles such as ASPNETCORE_URLS are not read into the BindingUrl
            //this is helpful if running AspNetCore in addition to a TcpService Server which will fight for the port
            //in Kubernetes the BindingUrl needs to be 0.0.0.0 which can be replaced with "+" such as "+:80"
            var serviceSettings = CQRSSettings.Get(false);

            IServiceCreator serviceCreator;

            //Option1: Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp (backend only services)
            serviceCreator = new TcpServiceCreator();

            //Option1B: Enable this for Http
            //var authorizer = new DemoCookieApiAuthorizer();
            //serviceCreator = new HttpServiceCreator(Zerra.CQRS.Network.ContentType.Json, authorizer);

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

            //Option3A: Enable this to start regular services
            Bus.StartServices(serviceSettings, serviceCreator);

            //Option3B: Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);

            timer.Stop();
            _ = Log.InfoAsync($"Startup Time {timer.ElapsedMilliseconds}ms");
        }
    }
}
