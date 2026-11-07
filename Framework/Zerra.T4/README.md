# Zerra.T4

T4 helpers for [Zerra](https://www.nuget.org/packages/Zerra), run from `.tt` templates in Visual Studio. **`MsSqlFirst`** reverse-engineers an existing SQL Server database into Zerra.Repository data models.

The JavaScript and TypeScript browser clients are no longer generated here. The gateway serves them with `app.UseCqrsClientScripts()`, see [Front End Scripts](https://github.com/szawaski/Zerra/blob/master/docs/FrontEndScripts.md).

## Installation

Zerra.T4 isn't a NuGet package. Copy the pre-built `Binaries/Zerra.T4.dll` into your solution, or build this project. T4 templates load assemblies by path, so point the template's `assembly` directive at `Zerra.T4.dll`.

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

Register a `TransactStoreProvider<TModel>` for each generated model.

## Documentation

- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Database First generation
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
