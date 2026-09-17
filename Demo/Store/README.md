# Store Demo

A small storefront split into five microservices behind one CQRS gateway. The web app serves static HTML and JavaScript pages that call the gateway with the Zerra front end scripts (`Bus.js`). Each service is a bounded context with its own data store (or none, by design), and it creates and seeds that store on startup.

```mermaid
flowchart LR
    Browser["Browser<br/>static pages + Bus.js"] -- "POST /CQRS (JSON)" --> Web["Store.Web<br/>CQRS API gateway"]
    Web -- "TCP" --> Catalog["Catalog service<br/>PostgreSQL"]
    Web -- "TCP" --> Inventory["Inventory service<br/>MySQL"]
    Web -- "TCP" --> Orders["Orders service<br/>SQL Server"]
    Web -- "TCP<br/>review commands: Azure Service Bus or TCP" --> Reviews["Reviews service<br/>MariaDB"]
    Web -- "HTTP" --> Shipping["Shipping service<br/>ASP.NET Core, in-memory"]
    Orders -- "query: GetProductsByIDs" --> Catalog
    Orders -- "command: ReserveStockCommand<br/>Kafka or TCP" --> Inventory
    Orders -. "event: OrderShippedEvent<br/>RabbitMQ or TCP" .-> Inventory
    Orders -. "event: OrderShippedEvent<br/>RabbitMQ or HTTP" .-> Shipping
    Reviews -- "query: GetProductsByIDs" --> Catalog
    Reviews -- "query: HasPurchased" --> Orders
```

Traffic between the gateway and most services, and between services, uses the binary serializer over TCP, encrypted with a shared key. Shipping is hosted inside ASP.NET Core, so its traffic is the same serializer and encryption over HTTP instead. Three flows go through a message broker when it's running, one per broker, and fall back to the direct route when it isn't (see [Message brokers](#message-brokers)). Browsers only talk JSON to the gateway.

## Projects

| Project | Role |
|---|---|
| `Store.Web` | ASP.NET app. Serves `wwwroot` and hosts `UseCqrsApiGateway("/CQRS")`. Its bus has no handlers, it forwards everything to the services. |
| `Store.Catalog.Domain` | Catalog contracts: `ICatalogQueryHandler`, `ICatalogCommandHandler`, commands, and models. |
| `Store.Catalog.Service` | Products and categories, PostgreSQL store. |
| `Store.Inventory.Domain` | Inventory contracts. `IStockReservationHandler` is for service-to-service use only. |
| `Store.Inventory.Service` | Stock levels, reservations, and the movement log, MySQL store. Subscribes to order events. |
| `Store.Orders.Domain` | Orders contracts, plus the events Orders publishes and `IOrderEventHandler` for subscribers. |
| `Store.Orders.Service` | Customers and orders, SQL Server store. Coordinates Catalog and Inventory, publishes to both Inventory and Shipping. |
| `Store.Shipping.Domain` | Shipping contracts: `IShippingQueryHandler`, `IShippingCommandHandler`. |
| `Store.Shipping.Service` | Tracks shipments. Hosted inside ASP.NET Core over HTTP/Kestrel instead of the raw TCP the other services use, and needs no database, it only reacts to events and holds state in memory. Subscribes to order events, the same interface Inventory subscribes to. |
| `Store.Reviews.Domain` | Reviews contracts: `IReviewsQueryHandler`, `IReviewsCommandHandler`. |
| `Store.Reviews.Service` | Product ratings and comments, MariaDB store. Calls Catalog for the product's name and Orders to mark a review a verified purchase. |
| `Store.Common` | Shared plumbing: settings, console loggers, `DomainException`, data store setup, the messaging description, and the seed product and customer IDs. |

Each service follows the same layout:

- `Handlers/`: the bus handlers. Command handlers check the rules, throwing `DomainException` with a message for the user, then read and write data models through `IRepo` and publish events. Query handlers read data models and map them to the contract models.
- `Data/`: data models, the `DataContextSelector` (or, for Shipping, a plain memory-only context), the store provider, and the seeder.

## Running

**Visual Studio (17.11 or later):** pick the **Store Demo (Databases, Message Brokers)** launch profile in the startup project dropdown and press F5. It starts the five services and the web app, which use the databases and message brokers when they're reachable, and the browser opens `http://localhost:5100`. The profiles are in `Zerra.slnLaunch` at the repository root. **Store Demo (In Memory, Direct Messaging)** starts them the same way with `STORE_IN_MEMORY` and `STORE_DIRECT_MESSAGING` set, so no databases or message brokers are needed. Each uses the profile with the same name, "Databases, Message Brokers" or "In Memory, Direct Messaging", in every project's `Properties/launchSettings.json`.

**Script:**

```powershell
.\Demo\Store\start-store.ps1                    # use the databases and message brokers when they're reachable
.\Demo\Store\start-store.ps1 -InMemory          # skip the databases
.\Demo\Store\start-store.ps1 -DirectMessaging   # skip the message brokers
```

It builds the six projects and starts each one in its own window.

**By hand:** `dotnet run` each of `Store.Catalog.Service`, `Store.Inventory.Service`, `Store.Orders.Service`, `Store.Shipping.Service`, `Store.Reviews.Service`, and `Store.Web`, in any order. Clients connect on first use.

**Native AOT:** every project (including the two ASP.NET Core ones, `Store.Web` and `Store.Shipping.Service`) sets `<PublishAot>true</PublishAot>`. Two scripts mirror `start-store.ps1`:

```powershell
.\Demo\Store\publish-store-aot.ps1            # publishes all six to .\Demo\Store\publish\<project>\<project>.exe
.\Demo\Store\start-store-aot.ps1 -InMemory    # starts the published executables, each in its own window
```

The publish script adds Visual Studio's installer folder to `PATH` for that run, since the native AOT linker needs `vswhere.exe` to find the VC++ toolchain and a plain shell usually doesn't have it on `PATH` (a Visual Studio Developer Command Prompt does). The run script sets `ASPNETCORE_URLS` for `Store.Web` and `Store.Shipping.Service`, since a published exe has no `launchSettings.json` to supply it.

## Data stores

Each service's `DataContextSelector` lists its database first and an in-memory store second. At startup the service checks whether the database is reachable. If it is, code-first generation creates the database and tables, and later adds columns. If not, the service logs why and runs in memory, reseeding on every start. Shipping has no `DataContextSelector`, it's memory-only by design (see [Where to look](#where-to-look)). The Overview page shows which store each service is using.

| Service | Database | Default connection |
|---|---|---|
| Catalog | PostgreSQL | `Host=localhost;Port=5432;User ID=postgres;Password=password123;Database=zerrastorecatalog` |
| Inventory | MySQL | `Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraStoreInventory` |
| Orders | SQL Server | `Data Source=.;Initial Catalog=ZerraStoreOrders;User ID=sa;Password=Password123`, then `Data Source=.;Initial Catalog=ZerraStoreOrders;Integrated Security=True` |
| Reviews | MariaDB | `Server=localhost;Port=3307;Uid=root;Pwd=password123;Database=ZerraStoreReviews` |
| Shipping | none, in-memory only | — |

Orders lists two SQL Server contexts ahead of the in-memory store: the SQL Server account first, which a SQL Server in Docker needs since it has no Windows authentication, then Windows authentication for a local install without that account.

To run all the databases in Docker, use `Demo/Infrastructure/start-infrastructure.ps1`. It starts only the ones that aren't already running locally, and `remove-infrastructure.ps1` removes them again.

Seeders only run against an empty store, so data in a database survives restarts. Drop a demo database to reseed it.

## Message brokers

Three flows between services use a different message broker each, and each falls back to the direct route when its broker isn't running:

| Flow | Broker | Without the broker |
|---|---|---|
| `ReserveStockCommand` from Orders to Inventory, awaited with a result | Kafka | TCP |
| `OrderShippedEvent` and `OrderCancelledEvent` from Orders to Inventory and Shipping | RabbitMQ, which delivers each event to both subscribers on its own | TCP to Inventory and HTTP to Shipping, one event producer each |
| `SubmitReviewCommand` from the gateway to Reviews, awaited with a result | Azure Service Bus (the emulator) | TCP |

At startup each service checks the brokers it uses with `KafkaConnection.TestAsync`, `RabbitMQConnection.Test`, or `AzureServiceBusConnection.TestAsync`, and logs which route it chose. The Overview page shows the consumers each service hosts, next to its data store. Outbound choices, such as Orders' and the gateway's, are only in their consoles. The sending service registers either the broker's producer or the direct client, and the receiving service makes the same check and registers either the broker's consumer or its TCP or Kestrel consumer, so both ends pick the same route.

Queries always go directly, brokers only carry commands and events.

To run the brokers in Docker, use `Demo/Infrastructure/start-infrastructure.ps1`, the same script that starts the databases.

## Settings

All settings have defaults in `Store.Common/StoreSettings.cs` and can be overridden with environment variables.

| Variable | Default |
|---|---|
| `STORE_IN_MEMORY` | not set. Set it to `true` to skip the databases. |
| `STORE_CATALOG_URL`, `STORE_INVENTORY_URL`, `STORE_ORDERS_URL`, `STORE_REVIEWS_URL` | `localhost:9101`, `localhost:9102`, `localhost:9103`, `localhost:9104` |
| `STORE_SHIPPING_URL` | `http://localhost:9105`, an HTTP endpoint since Shipping is hosted in ASP.NET Core |
| `STORE_CATALOG_POSTGRESQL`, `STORE_INVENTORY_MYSQL`, `STORE_ORDERS_MSSQL`, `STORE_ORDERS_MSSQL_WINDOWS_AUTH`, `STORE_REVIEWS_MARIADB` | the connection strings above |
| `STORE_DIRECT_MESSAGING` | not set. Set it to `true` to skip the message brokers. |
| `STORE_KAFKA` | `localhost:9092` |
| `STORE_RABBITMQ` | `localhost` |
| `STORE_AZURESERVICEBUS` | the Service Bus emulator from `Demo/Infrastructure`: `Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;` |
| `STORE_SHARED_KEY` | the demo key for the traffic encryption |

## Things to try

- Order 3 Standing Desks. Only 2 are in stock, so Inventory rejects the order. The error comes back through Orders and the gateway to the page.
- Place an order and open Inventory: the units are reserved. Ship the order and Inventory handles `OrderShippedEvent` by taking them off the shelf, or cancel it and `OrderCancelledEvent` releases them. The same `OrderShippedEvent` also reaches Shipping, which creates a tracking number independently, no coordination between the two subscribers.
- Open the Shipping page after shipping an order, then mark it delivered.
- Discontinue a product, then try to order it.
- Add a product, restock it, then order it.
- Write a review as a customer who received the product, then as one who never ordered it, and compare the Verified purchase badges on the Reviews page. The average rating then shows up next to that product on the Catalog page.
- Stop one service and use the pages to see how the failure surfaces. Stopping Shipping doesn't stop orders from shipping, Inventory still gets the event.
- Start the services with the brokers running and again with `-DirectMessaging`. The pages work the same either way, and each service's window shows which route it chose for each flow.

## Where to look

| Feature | Where |
|---|---|
| Gateway for browsers | `Store.Web/Program.cs` |
| Service host: handlers, TCP server, clients to other services | `Store.*.Service/Program.cs` |
| A service hosted in ASP.NET Core / Kestrel instead of raw TCP | `Store.Shipping.Service/Program.cs` (`KestrelCqrsServerQueryServer`, `KestrelCqrsServerCommandConsumer`, `KestrelCqrsServerEventConsumer`, `UseKestrelCqrsServer`), called from `Store.Web` and `Store.Orders.Service` with `KestrelCqrsClient` instead of `TcpCqrsClient` |
| A service with no database at all | `Store.Shipping.Service/Data/ShippingDataContext.cs`, a plain `MemoryDataContext` with no selector |
| Query called from another service | `OrdersCommandHandler` calls `ICatalogQueryHandler.GetProductsByIDs`; `ReviewsCommandHandler` calls both `ICatalogQueryHandler` and `IOrdersQueryHandler` |
| Command with a result, awaited across services | `PlaceOrderCommand`, and `ReserveStockCommand` from Orders to Inventory |
| Events between services, including two subscribers to the same event | Orders publishes `IOrderEventHandler` events through RabbitMQ, or without it registers two event producers, an Inventory `TcpCqrsClient` and a Shipping `KestrelCqrsClient`, and the bus sends each event to both (`Store.Orders.Service/Program.cs`); `OrderEventHandler` in each of `Store.Inventory.Service` and `Store.Shipping.Service` handles the notification its own way |
| Message brokers with a fallback to direct TCP/HTTP | Kafka in `Store.Orders.Service/Program.cs` and `Store.Inventory.Service/Program.cs`, RabbitMQ in those two and `Store.Shipping.Service/Program.cs`, Azure Service Bus in `Store.Web/Program.cs` and `Store.Reviews.Service/Program.cs` |
| Repository with a store per service and in-memory fallback | `Store.*.Service/Data/*DataContext.cs`, `Store.Common/Data/DataStoreSetup.cs` |
| Relations with `Graph` and partial updates | `CatalogQueryHandler` (product with category), `OrdersQueryHandler` (order with customer and lines), `CatalogCommandHandler`, `OrdersCommandHandler`, and `ShippingCommandHandler` (updates limited to the changed columns) |
| Service injection | `IDataStoreInfo` and `IMessagingInfo` added to `BusServices`, read by the `GetDataStoreName` and `GetMessagingName` queries |
| Data joined from two services in the browser | `catalog.js` joins Reviews' ratings onto Catalog's products; `orders.js` joins Shipping's tracking onto Orders' orders |
| Browser calls | `Store.Web/wwwroot/js/*.js`, using the generated `JavaScriptModels.js` |

## Regenerating the JavaScript models

`Store.Web/wwwroot/js/JavaScriptModels.js` is generated from the contracts in this folder by `JavaScriptModels.tt`, which uses `Front End Scripts/Binaries/Zerra.T4.dll`. Visual Studio runs the template when it's saved. The output is committed so the site runs without it.

## Simplifications

This is a demo, so some things a production system needs are left out:

- The gateway has no `ICqrsAuthorizer` and allows every origin. See [Security](../../docs/Security.md) and [Zerra.Web](../../docs/ZerraWeb.md).
- Events are published after the order is saved, without an outbox. If Inventory or Shipping is down when an order ships, the order is still marked shipped and that service never settles its side once it comes back.
- Inventory serializes stock changes with an in-process lock, which only works for a single instance.
- Each service checks the brokers only at startup and assumes the service on the other end of the flow makes the same choice. If a broker starts or stops while the services are running, restart the services on both ends of that flow, otherwise one end can be using the broker while the other uses the direct route. An awaited command sent through a broker also waits until it's handled rather than failing fast the way a TCP connection to a stopped service does.
