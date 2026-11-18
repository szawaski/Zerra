# Contributing to Zerra

Bug reports, fixes, and improvements are welcome.

## Reporting

- **Bugs:** open an issue with the package and version, what you expected, and the smallest code that reproduces it.
- **Security issues:** don't open an issue. Follow the [security policy](SECURITY.md).
- **Larger changes:** open an issue to discuss the design before writing the code. Zerra keeps a small core, so some features are left to applications.

## Building

You need the .NET 11 SDK. The solution builds with:

```bash
dotnet build Zerra.slnx
```

`Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0, so build that target after a change with `dotnet build -f netstandard2.0`.

## Testing

The test projects use xunit v3 and run as executables:

```bash
Tests/Zerra.Test/bin/Debug/net11.0/Zerra.Test.exe
```

`Zerra.Test` needs no infrastructure. The repository and messaging tests need the databases and brokers that `Demo/Infrastructure/start-infrastructure.ps1` starts in Docker. [Testing](docs/Testing.md) covers each test project.

Add a test for every fix that fails without it. Pull requests run the tests that need no infrastructure automatically.

## Code Conventions

The framework's conventions are in [Working on the Framework Itself](docs/Agents.md#working-on-the-framework-itself). The main ones:

- Runtime performance comes first. Make the smallest change in place and match the surrounding code rather than adding helpers or layers.
- Never block on async code. Add a real synchronous path instead.
- Runtime code stays Native AOT compatible.
- Comment only code that is critical or hard to follow.
- Files use CRLF line endings.
- Docs and the changelog stay short and say what users notice.

## Pull Requests

- Keep a pull request to one change, with its tests.
- Add a line to the [changelog](CHANGELOG.md) for anything users notice.
- Update the docs a change affects.

By contributing, you agree your contribution is licensed under the [MIT License](LICENSE).
