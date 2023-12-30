using System;
using Zerra;
using Zerra.CQRS.Relay;

namespace ZerraDemo.Relay
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.LoadConfiguration(args);
            var relayKey = Config.GetSetting("RelayKey") ?? throw new Exception("Missing Config RelayKey");
            var serverUrl = Config.GetSetting("server.urls") ?? throw new Exception("Missing Config server.urls");
            using (var tcpRelay = new TcpRelay(serverUrl, relayKey))
            {
                tcpRelay.Run();
            }
        }
    }
}
