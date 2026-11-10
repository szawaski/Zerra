[← Back to Documentation](Index.md)

# Front End Scripts

Browsers call the CQRS API gateway hosted by [Zerra.Web](ZerraWeb.md) with `Bus.js` or `Bus.ts`, using models, query functions, and commands built from your .NET contracts. The gateway serves all of them. `Demo/Store/Store.Web/wwwroot/` is a working site that uses them.

## Serving the Scripts

Add `UseCqrsClientScripts` next to the gateway:

```csharp
app.UseCqrsClientScripts("/CQRSClient", "/CQRS");     // the route the scripts are served under, and the gateway's route
app.UseCqrsApiGateway("/CQRS");
```

| Url | What it is |
|---|---|
| `/CQRSClient/Bus.js` | Client for the gateway, callback based, using `fetch` |
| `/CQRSClient/JavaScriptModels.js` | Models, query functions, and commands for `Bus.js` |
| `/CQRSClient/Bus.ts` | Client for the gateway, promise based, using `fetch` |
| `/CQRSClient/TypeScriptModels.ts` | Models, query classes, and commands for `Bus.ts` |

The served `Bus.js` and `Bus.ts` already call the gateway's route. The models are built from what the gateway's bus routes: a class per command, a function or static class per query interface, and a model type per model they use. They always match the running gateway, so there's nothing to regenerate after changing a contract.

Anyone who can reach the route sees the names and members of your queries, commands, and models. That's what shipped scripts show anyway, but if you save the TypeScript files, you only need the route in development.

How C# types appear in the scripts:

- Numbers are `number`, `DateTime` and `DateTimeOffset` are `Date`, and enums are their names as `string`.
- `Guid`, `char`, `TimeSpan`, `DateOnly`, `TimeOnly`, and `byte[]` are `string`, as they are in JSON.
- A `Stream` is a `Blob`. Dictionaries and `object` are `any`.
- Members follow the JSON serializer, including `[JsonIgnore]` and `[JsonPropertyName]`.
- A generic model is named with its type arguments, so `PagedResult<Order>` is `PagedResultOfOrder`.
- Query functions leave out the trailing `CancellationToken`.

The browser knows each query, command, and model only by its class name, so two with the same name in different namespaces fail the request with an error naming both. Rename one of them. A model also has a type map named with `Type` on the end, so the models `Order` and `OrderType` can't both be used. Names the scripts already use, such as `Date`, `Blob`, and `Bus`, fail the same way.

## Settings

Set these once when the page or app starts:

| Method | What it does |
|---|---|
| `Bus.setRoute("Gateway", url)` | Calls a different gateway, such as one on another site |
| `Bus.setRoute("IOrdersQueryHandler", url)` | Sends one query interface or command to its own gateway |
| `Bus.setHeader(name, value)` | Sends a header with every request. A function value is called for each request, such as for a token that changes |
| `Bus.onFail((message, url) => ...)` | Called for every failed query or command, after its own fail callback, such as to show a message |

## JavaScript

Load `Bus.js` before the models:

```html
<script src="/CQRSClient/Bus.js"></script>
<script src="/CQRSClient/JavaScriptModels.js"></script>

<script>
Bus.setHeader("X-API-Key", "my-secret-key");
Bus.onFail(function (message) { alert(message); });

// query
IUserQueryHandler.GetUser("12345", function (user) { ... }, function (errorText) { ... });

// command, waiting for the handler and its result
Bus.dispatchAwait(new CreateUserCommand({ Email: "user@example.com", Name: "John Doe" }),
    function (result) { ... }, function (errorText) { ... });

// command, fire and forget
Bus.dispatch(new UpdateUserCommand({ UserId: "12345", Name: "Jane Doe" }));
</script>
```

The fail callbacks receive the error message as a string. To call a query without a generated function, use `Bus.call(provider, method, args, modelType, hasMany, onComplete, onFail)`, where `modelType` is the generated model type and `hasMany` is `true` for arrays.

## TypeScript

Save `Bus.ts` and `TypeScriptModels.ts` side by side in the app, for example with an npm script:

```json
"scripts": {
    "zerra": "curl -o src/zerra/Bus.ts http://localhost:5000/CQRSClient/Bus.ts && curl -o src/zerra/TypeScriptModels.ts http://localhost:5000/CQRSClient/TypeScriptModels.ts"
}
```

Run it again after changing a contract. `Bus.ts` returns promises: `Bus.call(provider, method, args, modelType, hasMany)`, `Bus.dispatchAsync(command)`, and `Bus.dispatchAwaitAsync(command)`. The generated query classes wrap `Bus.call`.

```typescript
import { Bus } from "./zerra/Bus";
import { IUserQueryHandler, CreateUserCommand } from "./zerra/TypeScriptModels";

Bus.setRoute("Gateway", "https://myapp.example.com/CQRS");  // the saved Bus.ts calls the route it was served with
Bus.setHeader("X-API-Key", "my-secret-key");
Bus.onFail((message: string) => console.error(message));

const user = await IUserQueryHandler.GetUser("12345");
const result = await Bus.dispatchAwaitAsync(new CreateUserCommand({ Email: "user@example.com", Name: "John Doe" }));
await Bus.dispatchAsync(new CreateUserCommand({ Email: "other@example.com", Name: "Jane Doe" }));
```

A failed call rejects its promise with the error message.

## Streams

A query with a `Stream` parameter takes a `Blob` or `File`, uploaded after the other arguments in the same request. A query returning `Stream` resolves to a `Blob`:

```javascript
//Task<ImportPreviewModel> PreviewImport(Stream csv, CancellationToken cancellationToken)
IImportQueryHandler.PreviewImport(fileInput.files[0], function (preview) { ... });

//Task<Stream> ExportCsv(CancellationToken cancellationToken)
IImportQueryHandler.ExportCsv(function (blob) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "export.csv";
    link.click();
    URL.revokeObjectURL(url);
});
```

`Demo/Store`'s Export and import page (`catalog-import.html`) does both.

## Dates and Nameless JSON

Dates are sent as ISO 8601 strings and turned back into `Date` objects using the generated model types, including in nested objects and arrays.

When a call has a model type, the scripts ask for nameless JSON (`Accept: application/jsonnameless`), a compact form that sends values without property names, and rebuild the objects from the model type. The requests themselves are always standard JSON (`Content-Type: application/json`), so register the standard `NetworkJsonSerializer` with the gateway, not one with `Nameless = true`. Error responses are always standard JSON. See [Content Type Support](ZerraWeb.md#content-type-support).

## See Also

- [Zerra.Web](ZerraWeb.md) - The CQRS API gateway that serves and answers the scripts
- [JsonSerializer](JsonSerializer.md) - JSON and nameless JSON
