using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Zerra.CQRS.Network;
using Zerra.Serialization.Json;

namespace Zerra.CQRS.Relay
{
    public sealed class RelayRegister : IRelayRegister
    {
        private readonly string relayUrl;
        private readonly string relayKey;
        private readonly HttpClient client;
        public RelayRegister(string relayUrl, string relayKey)
        {
            this.relayUrl = relayUrl;
            this.relayKey = relayKey;
            this.client = new HttpClient();
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

        private async Task Send(string action, string serviceUrl, string[]? providerTypes)
        {
            var info = new ServiceInfo()
            {
                ProviderTypes = providerTypes,
                Url = serviceUrl
            };

            var json = JsonSerializer.Serialize(info);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var request = new HttpRequestMessage(HttpMethod.Post, relayUrl);

            request.Content = new WriteStreamContent(async (postStream) =>
            {
                await JsonSerializer.SerializeAsync(postStream, info);
            });
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(HttpCommon.ContentTypeJson);

            request.Headers.Add(HttpCommon.RelayServiceHeader, action);
            request.Headers.Add(HttpCommon.RelayKeyHeader, relayKey);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new WebException("Relay Register Failed");
            }
        }
    }
}
