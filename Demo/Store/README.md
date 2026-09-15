# Store Demo

A small storefront split into five microservices behind one CQRS gateway. The web app serves static HTML and JavaScript pages that call the gateway with the Zerra front end scripts (`Bus.js`). Each service is a bounded context with its own data store (or none, by design), and it creates and seeds that store on startup.

```mermaid
flowchart LR
    Browser["Browser<br/>static pages + Bus.js"] -- "POST /CQRS (JSON)" --> Web["Store.Web<br/>CQRS API gateway"]
    Web -- "TCP" --> Catalog["Catalog service<br/>PostgreSQL"]
    Web -- "TCP" --> Inventory["Inventory service<br/>MySQL"]
    Web -- "TCP" --> Orders["Orders service<br/>SQL Server"]
    Web -- "TCP" --> Reviews["Reviews service<br/>MariaDB"]
    Web -- "HTTP" --> Shipping["Shipping service<br/>ASP.NET Core, in-memory"]
    Orders -- "query: GetProductsByIDs" --> Catalog
    Orders -- "command: ReserveStockCommand" --> Inventory
    Orders -. "event: OrderShippedEvent" .-> Inventory
    Orders -. "event: OrderShippedEvent" .-> Shipping
    Reviews -- "query: GetProductsByIDs" --> Catalog
    Reviews -- "query: HasPurchased" --> Orders
```

Traffic between the gateway and most services, and between services, uses the binary serializer over TCP, encrypted with a shared key. Shipping is hosted inside ASP.NET Core, so its traffic is the same serializer and encryption over HTTP instead. Browsers only talk JSON to the gateway.

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
| `Store.Common` | Shared plumbing: settings, console loggers, `DomainException`, `MultiEventProducer`, data store setup, and the seed product and customer IDs. |

Each service follows the same layout:

- `Handlers/`: the bus handlers. Command handlers check the rules, throwing `DomainException` with a message for the user, then read and write data models through `IRepo` and publish events. Query handlers read data models and map them to the contract models.
- `Data/`: data models, the `DataContextSelector` (or, for Shipping, a plain memory-only context), the store provider, and the seeder.

## Running

**Visual Studio (17.11 or later):** pick the **Store Demo** launch profile in the startup project dropdown and press F5. It starts the five services and the web app, and the browser opens `http://localhost:5100`. The profile is in `Zerra.slnLaunch` at the repository root.

**Script:**

```powershell
.\Demo\Store\start-store.ps1            # use the databases when they're reachable
.\Demo\Store\start-store.ps1 -InMemory  # skip the databases
```

It builds the six projects and starts each one in its own window.

**By hand:** `dotnet run` each of `Store.Catalog.Service`, `Store.Inventory.Service`, `Store.Orders.Service`, `Store.Shipping.Service`, `Store.Reviews.Service`, and `Store.Web`, in any order. Clients connect on first use.

## Data stores

Each service's `DataContextSelector` lists its database first and an in-memory store second. At startup the service checks whether the database is reachable. If it is, code-first generation creates the database and tables, and later adds columns. If not, the service logs why and runs in memory, reseeding on every start. Shipping has no `DataContextSelector`, it's memory-only by design (see [Where to look](#where-to-look)). The Overview page shows which store each service is using.

| Service | Database | Default connection |
|---|---|---|
| Catalog | PostgreSQL | `Host=localhost;Port=5432;User ID=postgres;Password=password123;Database=zerrastorecatalog` |
| Inventory | MySQL | `Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraStoreInventory` |
| Orders | SQL Server | `Data Source=.;Initial Catalog=ZerraStoreOrders;Integrated Security=True` |
| Reviews | MariaDB | `Server=localhost;Port=3307;Uid=root;Pwd=password123;Database=ZerraStoreReviews` |
| Shipping | none, in-memory only | — |

Seeders only run against an empty store, so data in a database survives restarts. Drop a demo database to reseed it.

## Settings

All settings have defaults in `Store.Common/StoreSettings.cs` and can be overridden with environment variables.

| Variable | Default |
|---|---|
| `STORE_IN_MEMORY` | not set. Set it to `true` to skip the databases. |
| `STORE_CATALOG_URL`, `STORE_INVENTORY_URL`, `STORE_ORDERS_URL`, `STORE_REVIEWS_URL` | `localhost:9101`, `localhost:9102`, `localhost:9103`, `localhost:9104` |
| `STORE_SHIPPING_URL` | `http://localhost:9105`, an HTTP endpoint since Shipping is hosted in ASP.NET Core |
| `STORE_CATALOG_POSTGRESQL`, `STORE_INVENTORY_MYSQL`, `STORE_ORDERS_MSSQL`, `STORE_REVIEWS_MARIADB` | the connection strings above |
| `STORE_SHARED_KEY` | the demo key for the traffic encryption |

## Things to try

- Order 3 Standing Desks. Only 2 are in stock, so Inventory rejects the order. The error comes back through Orders and the gateway to the page.
- Place an order and open Inventory: the units are reserved. Ship the order and Inventory handles `OrderShippedEvent` by taking them off the shelf, or cancel it and `OrderCancelledEvent` releases them. The same `OrderShippedEvent` also reaches Shipping, which creates a tracking number independently, no coordination between the two subscribers.
- Open the Shipping page after shipping an order, then mark it delivered.
- Discontinue a product, then try to order it.
- Add a product, restock it, then order it.
- Write a review as a customer who received the product, then as one who never ordered it, and compare the Verified purchase badges on the Reviews page. The average rating then shows up next to that product on the Catalog page.
- Stop one service and use the pages to see how the failure surfaces. Stopping Shipping doesn't stop orders from shipping, Inventory still gets the event.

## Where to look

| Feature | Where |
|---|---|
| Gateway for browsers | `Store.Web/Program.cs` |
| Service host: handlers, TCP server, clients to other services | `Store.*.Service/Program.cs` |
| A service hosted in ASP.NET Core / Kestrel instead of raw TCP | `Store.Shipping.Service/Program.cs` (`KestrelCqrsServerQueryServer`, `KestrelCqrsServerCommandConsumer`, `KestrelCqrsServerEventConsumer`, `UseKestrelCqrsServer`), called from `Store.Web` and `Store.Orders.Service` with `KestrelCqrsClient` instead of `TcpCqrsClient` |
| A service with no database at all | `Store.Shipping.Service/Data/ShippingDataContext.cs`, a plain `MemoryDataContext` with no selector |
| Query called from another service | `OrdersCommandHandler` calls `ICatalogQueryHandler.GetProductsByIDs`; `ReviewsCommandHandler` calls both `ICatalogQueryHandler` and `IOrdersQueryHandler` |
| Command with a result, awaited across services | `PlaceOrderCommand`, and `ReserveStockCommand` from Orders to Inventory |
| Events between services, including two subscribers to the same event | Orders publishes `IOrderEventHandler` events through `Store.Common/MultiEventProducer.cs`, which composes an Inventory `TcpCqrsClient` and a Shipping `KestrelCqrsClient` behind one producer since the bus allows only one producer per event type; `OrderEventHandler` in each of `Store.Inventory.Service` and `Store.Shipping.Service` handles the notification its own way |
| Repository with a store per service and in-memory fallback | `Store.*.Service/Data/*DataContext.cs`, `Store.Common/Data/DataStoreSetup.cs` |
| Relations with `Graph` and partial updates | `CatalogQueryHandler` (product with category), `OrdersQueryHandler` (order with customer and lines), `CatalogCommandHandler`, `OrdersCommandHandler`, and `ShippingCommandHandler` (updates limited to the changed columns) |
| Service injection | `IDataStoreInfo` added to `BusServices`, read by the `GetDataStoreName` queries |
| Data joined from two services in the browser | `catalog.js` joins Reviews' ratings onto Catalog's products; `orders.js` joins Shipping's tracking onto Orders' orders |
| Browser calls | `Store.Web/wwwroot/js/*.js`, using the generated `JavaScriptModels.js` |

## Regenerating the JavaScript models

`Store.Web/wwwroot/js/JavaScriptModels.js` is generated from the contracts in this folder by `JavaScriptModels.tt`, which uses `Front End Scripts/Binaries/Zerra.T4.dll`. Visual Studio runs the template when it's saved. The output is committed so the site runs without it.

## Simplifications

This is a demo, so some things a production system needs are left out:

- The gateway has no `ICqrsAuthorizer` and allows every origin. See [Security](../../docs/Security.md) and [Zerra.Web](../../docs/ZerraWeb.md).
- Events are published after the order is saved, without an outbox. If Inventory or Shipping is down when an order ships, the order is still marked shipped and that service never settles its side once it comes back.
- Inventory serializes stock changes with an in-process lock, which only works for a single instance.
