using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.Relay
{
    public sealed class RelayRegister : IRelayRegister
    {
        private readonly string relayUrl;
        private readonly string relayKey;
        public RelayRegister(string relayUrl, string relayKey)
        {
            this.relayUrl = relayUrl;
            this.relayKey = relayKey;
        }

        public string RelayUrl => relayUrl;

        public Task Register(string serviceUrl, string[] providerTypes)
        {
            return Send(HttpCommon.RelayServiceAdd, serviceUrl, providerTypes);
        }

        public Task Unregister(string serviceUrl)
        {
            return Send(HttpCommon.RelayServiceRemove, serviceUrl, null);
        }

        private async Task Send(string action, string serviceUrl, string[] providerTypes)
        {
            var info = new ServiceInfo()
            {
                ProviderTypes = providerTypes,
                Url = serviceUrl
            };

            var json = JsonSerializer.Serialize(info);
            var bytes = Encoding.UTF8.GetBytes(json);

            var request = WebRequest.CreateHttp(relayUrl);
            request.Method = "POST";
            request.ContentLength = bytes.Length;
            request.Headers.Add(HttpCommon.RelayServiceHeader, action);
            request.Headers.Add(HttpCommon.RelayKeyHeader, relayKey);
            using (var postStream = request.GetRequestStream())
            {
                await postStream.WriteAsync(bytes, 0, bytes.Length);
                await postStream.FlushAsync();
            };

            using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException("Relay Register Failed");
                }
            }
        }
    }
}
