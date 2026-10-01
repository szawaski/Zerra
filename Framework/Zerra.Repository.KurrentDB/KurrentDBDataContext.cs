// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.Repository.KurrentDB
{
    /// <summary>
    /// Creates KurrentDB engines and checks whether a KurrentDB node can be reached, such as at startup to choose between KurrentDB and another event store.
    /// </summary>
    public static class KurrentDBDataContext
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Creates an engine for the KurrentDB node. Dispose it when the application stops, it holds the client's connections.
        /// </summary>
        /// <param name="connectionString">The connection string for the KurrentDB instance.</param>
        /// <param name="insecure">Whether the node runs without TLS/SSL.</param>
        /// <returns>The engine to give the aggregates and store providers.</returns>
        public static KurrentDBEngine GetEngine(string connectionString, bool insecure)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return new KurrentDBEngine(connectionString, insecure);
        }

        /// <summary>
        /// Tests the connection with the node's HTTP health check. This is synchronous because the KurrentDB client only has async calls, so the health check is used instead.
        /// </summary>
        /// <param name="connectionString">The connection string for the KurrentDB instance.</param>
        /// <param name="insecure">Whether the node runs without TLS/SSL.</param>
        /// <param name="timeout">How long to wait for the health check, five seconds if not given.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the health check answered; otherwise false.</returns>
        public static bool TestConnection(string connectionString, bool insecure, TimeSpan? timeout = null, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            try
            {
                var address = new Uri(connectionString);
                var healthUri = new UriBuilder(address) { Scheme = insecure ? Uri.UriSchemeHttp : address.Scheme, Path = "/health/live", Query = String.Empty }.Uri;

                using var httpClient = new HttpClient() { Timeout = timeout ?? defaultTimeout };
                using var request = new HttpRequestMessage(HttpMethod.Get, healthUri);
                using var response = httpClient.Send(request);
                if (response.IsSuccessStatusCode)
                    return true;

                log?.Warn($"{nameof(KurrentDBDataContext)} could not connect: health check returned {(int)response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(KurrentDBDataContext)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}
