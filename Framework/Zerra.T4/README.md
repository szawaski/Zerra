# Zerra.T4

T4 helpers for [Zerra](https://www.nuget.org/packages/Zerra), run from `.tt` templates in Visual Studio:

- **`CQRSClientDomain`** generates JavaScript or TypeScript models, commands, and query functions from your C# contracts, for the `Bus.js` and `Bus.ts` clients that call the Zerra.Web API gateway.
- **`MsSqlFirst`** reverse-engineers an existing SQL Server database into Zerra.Repository data models and providers.

## Installation

```bash
dotnet add package Zerra.T4
```

T4 templates load assemblies by path, so point the template's `assembly` directive at `Zerra.T4.dll`.

## Front End Models

```
<#@ template language="C#" debug="false" hostspecific="true" #>
<#@ output extension=".ts" #>
<#@ assembly name="Zerra.T4.dll" #>
<#
    var directory = this.Host.ResolvePath(@"..\..\MyProject.Domain");
    var result = Zerra.T4.CQRSClientDomain.GenerateTypeScript(directory);
    #><#=result#><#
#>
```

`GenerateJavaScript(directory)` writes the JavaScript equivalent.

## Database First Models

```
<#@ template language="C#" debug="false" hostspecific="true" #>
<#@ assembly name="Zerra.T4.dll" #>
<#
    const string connectionString = "data source=.;initial catalog=MyDatabase;integrated security=True;";
    var result = Zerra.T4.MsSqlFirst.GenerateModels(connectionString, "MyProject.Domain.DataModels", "DataModel");
    #><#=result#><#
#>
```

`MsSqlFirst.GenerateProviders` writes a typed provider per model.

## Documentation

- [Front End Scripts](https://github.com/szawaski/Zerra/blob/master/docs/FrontEndScripts.md) - The browser clients and model templates
- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Database First generation
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
