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

        public static GeneratorOutput Run(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var compilation = CSharpCompilation.Create("ZerraSourceGenerationTestAssembly", [syntaxTree], references, new CSharpCompilationOptions(outputKind, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));

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

            return new GeneratorOutput(sources, outputCompilation);
        }
    }

    public sealed class GeneratorOutput
    {
        public IReadOnlyDictionary<string, string> Sources { get; }
        public string Initializer => Sources["ZerraSourceGenerationInitializer.cs"];
        private readonly Compilation compilation;

        public GeneratorOutput(IReadOnlyDictionary<string, string> sources, Compilation compilation)
        {
            this.Sources = sources;
            this.compilation = compilation;
        }

        //loads the source with its generated code and runs the generated registrations, so the Zerra types in the test host use them
        public System.Reflection.Assembly Load()
        {
            using var stream = new MemoryStream();
            var emit = compilation.Emit(stream);
            Assert.True(emit.Success, String.Join(Environment.NewLine, emit.Diagnostics));
            var assembly = System.Reflection.Assembly.Load(stream.ToArray());
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
            return assembly;
        }

        //runs the source, built with OutputKind.ConsoleApplication, in its own process with dynamic code turned off as native AOT has it,
        //so any type detail or converter the generator missed throws instead of being made through reflection
        //the switch is read once when the process starts, which is why it can't be done in the test host
        //this does not trim or compile ahead of time, so it finds what the generator misses, not what trimming removes
        public (int ExitCode, string Output) RunWithoutDynamicCode()
        {
            Assert.Equal(OutputKind.ConsoleApplication, compilation.Options.OutputKind);

            var directory = Path.Combine(Path.GetTempPath(), "ZerraNoDynamicCode", Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(directory);
            try
            {
                var assemblyPath = Path.Combine(directory, $"{compilation.AssemblyName}.dll");
                var emit = compilation.Emit(assemblyPath);
                Assert.True(emit.Success, String.Join(Environment.NewLine, emit.Diagnostics));

                foreach (var zerraFile in new string[] { typeof(Zerra.CQRS.ICommand).Assembly.Location, typeof(Zerra.Repository.AggregateRoot).Assembly.Location })
                    File.Copy(zerraFile, Path.Combine(directory, Path.GetFileName(zerraFile)));

                //the same runtime the test host runs on
                var hostConfig = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, $"{typeof(GeneratorRunner).Assembly.GetName().Name}.runtimeconfig.json")))!;
                var runtimeOptions = new System.Text.Json.Nodes.JsonObject()
                {
                    ["tfm"] = hostConfig["runtimeOptions"]!["tfm"]!.DeepClone(),
                    ["framework"] = hostConfig["runtimeOptions"]!["framework"]!.DeepClone(),
                    ["configProperties"] = new System.Text.Json.Nodes.JsonObject() { ["System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"] = false }
                };
                File.WriteAllText(Path.Combine(directory, $"{compilation.AssemblyName}.runtimeconfig.json"), new System.Text.Json.Nodes.JsonObject() { ["runtimeOptions"] = runtimeOptions }.ToJsonString());

                var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", $"\"{assemblyPath}\"")
                {
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var process = new System.Diagnostics.Process() { StartInfo = startInfo };
                var standardError = new StringBuilder();
                process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (standardError) _ = standardError.AppendLine(e.Data); };
                _ = process.Start();
                process.BeginErrorReadLine();
                var standardOutput = process.StandardOutput.ReadToEnd();
                Assert.True(process.WaitForExit(60000), "The app did not exit");
                //waits for the error output to finish
                process.WaitForExit();
                return (process.ExitCode, standardOutput + standardError.ToString());
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        public IEnumerable<string> Lines(Func<string, bool> predicate) => Initializer.Split('\n').Where(predicate);

        public void AssertInitializerContains(string text) => Assert.True(Initializer.Contains(text), $"Initializer missing:{Environment.NewLine}{text}{Environment.NewLine}{Environment.NewLine}{Initializer}");
        public void AssertInitializerDoesNotContain(string text) => Assert.False(Initializer.Contains(text), $"Initializer should not contain:{Environment.NewLine}{text}{Environment.NewLine}{Environment.NewLine}{Initializer}");
    }
}
