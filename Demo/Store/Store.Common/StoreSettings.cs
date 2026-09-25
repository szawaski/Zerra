using Zerra.Encryption;
using Zerra.Serialization;

namespace Store.Common
{
    /// <summary>
    /// Addresses, connection strings, and shared secrets for the Store demo.
    /// Every value has a local default and can be overridden with an environment variable.
    /// </summary>
    public static class StoreSettings
    {
        //TCP addresses of the microservices
        public static string CatalogServiceUrl => Get("STORE_CATALOG_URL", "localhost:9101");
        public static string InventoryServiceUrl => Get("STORE_INVENTORY_URL", "localhost:9102");
        public static string OrdersServiceUrl => Get("STORE_ORDERS_URL", "localhost:9103");
        public static string ReviewsServiceUrl => Get("STORE_REVIEWS_URL", "localhost:9104");
        public static string CartsServiceUrl => Get("STORE_CARTS_URL", "localhost:9106");

        //Shipping is hosted in ASP.NET Core, so its address is an HTTP endpoint rather than a bare TCP host:port
        public static string ShippingServiceUrl => Get("STORE_SHIPPING_URL", "http://localhost:9105");

        //Each service owns its own data store, a short connect timeout keeps the in-memory fallback quick when the database isn't running
        public static string CatalogPostgreSql => Get("STORE_CATALOG_POSTGRESQL", "Host=localhost;Port=5432;User ID=postgres;Password=password123;Database=zerrastorecatalog;Timeout=3");
        public static string InventoryMySql => Get("STORE_INVENTORY_MYSQL", "Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraStoreInventory;Connect Timeout=3");
        public static string OrdersMsSql => Get("STORE_ORDERS_MSSQL", "Data Source=.;Initial Catalog=ZerraStoreOrders;User ID=sa;Password=Password123;TrustServerCertificate=True;Connect Timeout=3");
        //used when the SQL Server account can't log in, e.g. a local install without it
        public static string OrdersMsSqlWindowsAuth => Get("STORE_ORDERS_MSSQL_WINDOWS_AUTH", "Data Source=.;Initial Catalog=ZerraStoreOrders;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3");
        public static string ReviewsMariaDb => Get("STORE_REVIEWS_MARIADB", "Server=localhost;Port=3307;Uid=root;Pwd=password123;Database=ZerraStoreReviews;Connect Timeout=3");
        //KurrentDB's gRPC and HTTP share the node port, the demo container runs without TLS
        public static string CartsKurrentDB => Get("STORE_CARTS_KURRENTDB", "http://localhost:2113");

        /// <summary>
        /// Skip the databases and use the in-memory stores, set STORE_IN_MEMORY=true.
        /// </summary>
        public static bool InMemoryOnly => String.Equals(Environment.GetEnvironmentVariable("STORE_IN_MEMORY"), "true", StringComparison.OrdinalIgnoreCase);

        //Message brokers, each carries one flow between services when it's reachable at startup, otherwise that flow goes directly over TCP or HTTP
        public static string KafkaHost => Get("STORE_KAFKA", "localhost:9092");
        public static string RabbitMQHost => Get("STORE_RABBITMQ", "localhost");
        //the Service Bus emulator from Demo/Infrastructure, its AMQP port is moved to 5673 since RabbitMQ owns 5672
        public static string AzureServiceBusConnectionString => Get("STORE_AZURESERVICEBUS", "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");

        /// <summary>
        /// Skip the message brokers and send everything directly between services, set STORE_DIRECT_MESSAGING=true.
        /// </summary>
        public static bool DirectMessagingOnly => String.Equals(Environment.GetEnvironmentVariable("STORE_DIRECT_MESSAGING"), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Serializer for traffic between the gateway and the services, and between services.
        /// </summary>
        public static ISerializer CreateServiceSerializer() => new ZerraByteSerializer();

        /// <summary>
        /// Internal traffic is encrypted with a shared key so only callers holding the key can send messages to a service.
        /// A real deployment would load this from a secret store.
        /// </summary>
        public static IEncryptor CreateServiceEncryptor() => new ZerraEncryptor(Get("STORE_SHARED_KEY", "zerra-store-demo-shared-key"), SymmetricAlgorithmType.AESwithPrefix);

        private static string Get(string name, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
