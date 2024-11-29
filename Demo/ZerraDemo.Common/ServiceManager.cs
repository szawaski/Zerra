using System.Diagnostics;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.Settings;
using Zerra.Logging;
using Zerra.Logger;
using System;

namespace ZerraDemo.Common
{
    public static class ServiceManager
    {
        public static void StartServices(Action? loader = null, int? receiveCommandsBeforeExit = null)
        {
            var timer = Stopwatch.StartNew();
            loader?.Invoke();
            Bus.ReceiveCommandsBeforeExit = receiveCommandsBeforeExit;

            Bus.AddLogger(new BusLoggingProvider());

            //bindingUrlFromStandardVariables = false means that
            //environmental varibles such as ASPNETCORE_URLS are not read into the BindingUrl
            //this is helpful if running AspNetCore in addition to a TcpService Server which will fight for the port
            //in Kubernetes the BindingUrl needs to be 0.0.0.0 which can be replaced with "+" such as "+:80"
            var serviceSettings = CQRSSettings.Get(false);

            //var messageHost = Config.GetSetting("MessageHost")!;
            //serviceSettings.SetAllMessageHosts(messageHost);

            IServiceCreator serviceCreator;

            //Option1: Enable one of the following service options
            //----------------------------------------------------------

            //Option1A: Enable this for Tcp (backend only services)
            serviceCreator = new TcpServiceCreator();

            //Option1B: Enable this for Http
            //var authorizer = new DemoCookieApiAuthorizer();
            //serviceCreator = new HttpServiceCreator(Zerra.CQRS.Network.ContentType.Json, authorizer);

            //Option1C: Enable this using Kestrel Http (Required for Azure Apps)
            //var serviceCreator = new Zerra.Web.KestrelServiceCreator(app, ContentType.Bytes);

            //Option2: Enable one of the following message services
            //----------------------------------------------------------

            //Option2A: Enable this using RabbitMQ for commands/events
            //serviceCreator = new Zerra.CQRS.RabbitMQ.RabbitMQServiceCreator(serviceCreator, Config.EnvironmentName);

            //Option2B: Enable this using Kafka for commands/events
            //serviceCreator = new Zerra.CQRS.Kafka.KafkaServiceCreator(serviceCreator, Config.EnvironmentName);

            //Option2C: Enable this using Azure Event Hub for commands/events
            //serviceCreator = new Zerra.CQRS.AzureEventHub.AzureEventHubServiceCreator(serviceCreator, Config.EnvironmentName);

            //Option2D: Enable this using Azure Service Bus for commands/events
            //serviceCreator = new Zerra.CQRS.AzureServiceBus.AzureServiceBusServiceCreator(serviceCreator, Config.EnvironmentName);

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
