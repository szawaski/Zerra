// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System;
using System.IO;
using System.Linq;

namespace Zerra.SourceGeneration.Test
{
    public class RunGeneratorTests
    {
        [Fact]
        public void Test()
        {
            RunGenerator(10.0m);
        }

        private static void RunGenerator(decimal netVersion)
        {
            var netVersionString = netVersion.ToString("0.0");
            var directive = $"NET{netVersionString.Replace('.', '_')}";

            var netCorePath = DirectoryHelper.NetCoreDirectory;
            var folders = netCorePath.GetDirectories($"{netVersionString}.*");
            var netCoreVersionPath = folders.OrderByDescending(x => Int32.Parse(x.Name.Split('.').Last())).FirstOrDefault();
            if (netCoreVersionPath is null)
                throw new InvalidOperationException("NetCore Version not found");

            var referenceFiles = new string[]
            {
                $"{netCoreVersionPath}\\System.Private.CoreLib.dll",
                $"{netCoreVersionPath}\\System.Runtime.dll"
            };
            var references = referenceFiles.Select(x => MetadataReference.CreateFromFile(x)).ToArray();

            var filePathToRead = $"{DirectoryHelper.SolutionDirectory}{Path.DirectorySeparatorChar}Demo";
            var filesToRead = Directory.GetFiles(filePathToRead, "*.cs", SearchOption.AllDirectories).Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")).ToArray();
            var syntaxTrees = filesToRead.Select(x => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(x))).ToArray();

            var compilation = CSharpCompilation.Create("ZerraSourceGenerationTestAssembly", syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var sourceGenerator = new ZerraIncrementalGenerator().AsSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

            var driverResult = driver.RunGenerators(compilation);
            var results = driverResult.GetRunResult();
            var result = results.Results.Single();

            //var path = $"C:{Path.DirectorySeparatorChar}Temp{Path.DirectorySeparatorChar}SourceGenerationTests";
            //if (!Directory.Exists(path))
            //    _ = Directory.CreateDirectory(path);
            //path = $"{path}{Path.DirectorySeparatorChar}{directive}";
            //if (!Directory.Exists(path))
            //    _ = Directory.CreateDirectory(path);

            //foreach (var source in result.GeneratedSources)
            //{
            //    var filePath = $"{path}{Path.DirectorySeparatorChar}{source.HintName}";
            //    File.WriteAllText(filePath, source.SourceText.ToString(), source.SourceText.Encoding ?? Encoding.UTF8);
            //}

            //var sourceFiles = result.GeneratedSources.Select(x => x.HintName).ToHashSet();
            //foreach (var file in Directory.GetFiles(path).Where(x => !sourceFiles.Contains(x.Split(Path.DirectorySeparatorChar).Last())))
            //    File.Delete(file);
        }
    }
}
