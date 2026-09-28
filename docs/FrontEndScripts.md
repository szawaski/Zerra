[← Back to Documentation](Index.md)

# Front End Scripts

The **Front End Scripts** folder holds JavaScript and TypeScript clients for browsers. They call the CQRS API gateway hosted by [Zerra.Web](ZerraWeb.md), using models generated from your .NET contracts. `Demo/Store/Store.Web/wwwroot/js/` is a working site that uses them.

## Files

| File | Purpose |
|---|---|
| [`JavaScript/Bus.js`](../Front%20End%20Scripts/JavaScript/Bus.js) | Client for the gateway, callback based. Requires jQuery |
| [`JavaScript/BusRoutes.js`](../Front%20End%20Scripts/JavaScript/BusRoutes.js) | Gateway routes and the global error handler |
| [`JavaScript/JavaScriptModels.tt`](../Front%20End%20Scripts/JavaScript/JavaScriptModels.tt) | T4 template generating JavaScript models and query functions |
| [`TypeScript/Bus.ts`](../Front%20End%20Scripts/TypeScript/Bus.ts) | Client for the gateway, promise based, using `fetch` |
| [`TypeScript/BusConfig.ts`](../Front%20End%20Scripts/TypeScript/BusConfig.ts) | Gateway routes and the global error handler |
| [`TypeScript/TypeScriptModels.tt`](../Front%20End%20Scripts/TypeScript/TypeScriptModels.tt) | T4 template generating TypeScript types and query classes |
| [`Binaries/Zerra.T4.dll`](../Front%20End%20Scripts/Binaries/Zerra.T4.dll) | The generator the templates call, built from `Framework/Zerra.T4` |

## Generating Models

The templates call `Zerra.T4.CQRSClientDomain.GenerateJavaScript(folder)` or `GenerateTypeScript(folder)`, which read the C# sources under `folder` and write a class per command, a function or static class per query interface, and a model type per model the contracts use.

1. Copy `Bus.js` or `Bus.ts`, its routes file, the template, and the `Binaries` folder into your solution.
2. Point the template's `assembly` directive at `Zerra.T4.dll`, for example `<#@ assembly name="..\Binaries\Zerra.T4.dll" #>`.
3. Set the folder it scans. The shipped template scans the whole solution folder. Narrow it to your own `*.Domain` projects so unrelated types aren't picked up, as `Demo/Store/Store.Web/wwwroot/js/JavaScriptModels.tt` does.
4. Save the template in Visual Studio to run it, and run it again after changing a contract.

To run the template on every build instead, add the text templating targets to the web project (see [`Help-ProjectBuildT4.txt`](../Front%20End%20Scripts/JavaScript/Help-ProjectBuildT4.txt)):

```xml
<Import Project="$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v17.0\TextTemplating\Microsoft.TextTemplating.targets" />
<PropertyGroup>
    <TransformOnBuild>true</TransformOnBuild>
    <OverwriteReadOnlyOutputFiles>true</OverwriteReadOnlyOutputFiles>
    <TransformOutOfDateOnly>false</TransformOutOfDateOnly>
</PropertyGroup>
<ItemGroup>
    <None Include="..\Scripts\TypeScriptModels.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <OutputFilePath>..\MyWebApp\src\services</OutputFilePath>
        <LastGenOutput>TypeScriptModels.ts</LastGenOutput>
    </None>
</ItemGroup>
```

Generated query functions leave out the trailing `CancellationToken`.

## JavaScript

```html
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script src="JavaScriptModels.js"></script>
<script src="Bus.js"></script>
<script src="BusRoutes.js"></script>

<script>
BusRoutes["Gateway"] = "/CQRS";                            // required; a route keyed by an interface name overrides it for that interface
Bus.setHeader("X-API-Key", "my-secret-key");               // sent with every request

// query
IUserQueryHandler.GetUser("12345", function (user) { ... }, function (errorText) { ... });

// command, waiting for the handler and its result
Bus.DispatchAwait(new CreateUserCommand({ Email: "user@example.com", Name: "John Doe" }),
    function (result) { ... }, function (errorText) { ... });

// command, fire and forget
Bus.Dispatch(new UpdateUserCommand({ UserId: "12345", Name: "Jane Doe" }));
</script>
```

`onFail` receives the error message as a string. For a global handler, edit the `BusFail` function in `BusRoutes.js`; it is a `const`, so page script can't reassign it.

To call a query without a generated function, use `Bus.Call(provider, method, args, modelType, hasMany, onComplete, onFail)`, where `modelType` is the generated model type and `hasMany` is `true` for arrays.

## TypeScript

`Bus.ts` returns promises: `Bus.Call(provider, method, args, modelType, hasMany)`, `Bus.DispatchAsync(command)`, `Bus.DispatchAwaitAsync(command)`, and `Bus.SetHeader(header, value)`. The generated query classes wrap `Bus.Call`.

```typescript
import { Bus } from "./Bus";
import { SetBusRoute, SetBusFailCallback } from "./BusConfig";
import { IUserQueryHandler, CreateUserCommand } from "./TypeScriptModels";

SetBusRoute("Gateway", "https://myapp.example.com/CQRS");  // defaults to /CQRS
Bus.SetHeader("X-API-Key", "my-secret-key");
SetBusFailCallback((message: string) => console.error(message));

const user = await IUserQueryHandler.GetUser("12345");
const result = await Bus.DispatchAwaitAsync(new CreateUserCommand({ Email: "user@example.com", Name: "John Doe" }));
await Bus.DispatchAsync(new CreateUserCommand({ Email: "other@example.com", Name: "Jane Doe" }));
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
    $("<a>").attr({ href: url, download: "export.csv" })[0].click();
    URL.revokeObjectURL(url);
});
```

`Demo/Store`'s Export and import page (`catalog-import.html`) does both.

## Dates and Nameless JSON

Dates are sent as ISO 8601 strings and turned back into `Date` objects using the generated model types, including in nested objects and arrays.

When a call has a model type, the scripts ask for nameless JSON (`Accept: application/jsonnameless`), a compact form that sends values without property names, and rebuild the objects from the model type. The requests themselves are always standard JSON (`Content-Type: application/json`), so register the standard `ZerraJsonSerializer` with the gateway, not one with `Nameless = true`. Error responses are always standard JSON. See [Content Type Support](ZerraWeb.md#content-type-support).

## See Also

- [Zerra.Web](ZerraWeb.md) - The CQRS API gateway the scripts call
- [JsonSerializer](JsonSerializer.md) - JSON and nameless JSON
