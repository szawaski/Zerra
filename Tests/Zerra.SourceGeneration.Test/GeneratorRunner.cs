// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Zerra.SourceGeneration.Test
{
    /// <summary>
    /// Runs the generator over source text compiled against the runtime and the real Zerra assemblies,
    /// then checks the generator did not throw and that the generated code compiles with the source.
    /// </summary>
    public static class GeneratorRunner
    {
        private static readonly MetadataReference[] references = GetReferences();
        private static MetadataReference[] GetReferences()
        {
            //only the runtime's own assemblies, the test host's list also holds Roslyn, xunit and the generator itself
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var runtimeFiles = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(x => String.Equals(Path.GetDirectoryName(x), runtimeDirectory, StringComparison.OrdinalIgnoreCase));

            var zerraFiles = new string[]
            {
                typeof(Zerra.CQRS.ICommand).Assembly.Location,
                typeof(Zerra.Repository.AggregateRoot).Assembly.Location
            };

            return runtimeFiles.Concat(zerraFiles).Select(x => MetadataReference.CreateFromFile(x)).ToArray();
        }

        public static GeneratorOutput Run(string source)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var compilation = CSharpCompilation.Create("ZerraSourceGenerationTestAssembly", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));

            var inputErrors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
            Assert.True(inputErrors.Length == 0, $"Test source does not compile:{Environment.NewLine}{String.Join(Environment.NewLine, inputErrors.Select(x => x.ToString()))}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create([new ZerraIncrementalGenerator().AsSourceGenerator()], parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            var result = driver.GetRunResult().Results.Single();
            Assert.Null(result.Exception);
            Assert.Empty(generatorDiagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));

            var sources = result.GeneratedSources.ToDictionary(x => x.HintName, x => x.SourceText.ToString());

            //nullable warnings (CS86xx, CS87xx) in the generated files count too: projects built with TreatWarningsAsErrors get them as errors
            var outputErrors = outputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error || (x.Severity == DiagnosticSeverity.Warning && (x.Id.StartsWith("CS86") || x.Id.StartsWith("CS87")) && x.Location.SourceTree is not null && x.Location.SourceTree != syntaxTree)).ToArray();
            if (outputErrors.Length > 0)
            {
                var sb = new StringBuilder();
                _ = sb.AppendLine("Generated code does not compile cleanly:");
                foreach (var error in outputErrors)
                    _ = sb.AppendLine(error.ToString());
                foreach (var file in outputErrors.Select(x => x.Location.SourceTree?.FilePath).Where(x => x is not null).Distinct())
                {
                    var hintName = Path.GetFileName(file!);
                    if (sources.TryGetValue(hintName, out var text))
                        _ = sb.AppendLine().AppendLine($"--- {hintName} ---").AppendLine(text);
                }
                Assert.Fail(sb.ToString());
            }

            return new GeneratorOutput(sources);
        }
    }

    public sealed class GeneratorOutput
    {
        public IReadOnlyDictionary<string, string> Sources { get; }
        public string Initializer => Sources["ZerraSourceGenerationInitializer.cs"];

        public GeneratorOutput(IReadOnlyDictionary<string, string> sources)
        {
            this.Sources = sources;
        }

        public IEnumerable<string> Lines(Func<string, bool> predicate) => Initializer.Split('\n').Where(predicate);

        public void AssertInitializerContains(string text) => Assert.True(Initializer.Contains(text), $"Initializer missing:{Environment.NewLine}{text}{Environment.NewLine}{Environment.NewLine}{Initializer}");
        public void AssertInitializerDoesNotContain(string text) => Assert.False(Initializer.Contains(text), $"Initializer should not contain:{Environment.NewLine}{text}{Environment.NewLine}{Environment.NewLine}{Initializer}");
    }
}
