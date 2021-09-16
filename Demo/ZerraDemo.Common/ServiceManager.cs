using System;
using System.Linq;
using Zerra;
using Zerra.CQRS;
using Zerra.CQRS.Relay;
using Zerra.Logger;

namespace ZerraDemo.Common
{
    public static class ServiceManager
    {
        public static void StartServices()
        {
            var settingsName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

            var serviceSettings = Zerra.CQRS.Settings.CQRSSettings.Get();

            Bus.AddMessageLogger(new MessageLoggingProvider());

            //Enable this for Http which can be access directly from a front end
            //var authorizor = new DemoCookieApiAuthorizer();
            //var serviceCreator = new TcpApiServiceCreator(authorizor, null);

            //Enable this for Tcp for backend only services
            var serviceCreator = new TcpInternalServiceCreator();

            //Enable this to use the relay/loadbalancer
            //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
            //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);

            //Enable this for direct service communication, no relay/loadbalancer
            Bus.StartServices(settingsName, serviceSettings, serviceCreator, null);
        }
    }
}
