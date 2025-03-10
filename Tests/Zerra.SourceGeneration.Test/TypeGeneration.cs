﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Zerra.SourceGeneration.Test
{
    [TestClass]
    public class TypeGeneration
    {
        //[TestMethod]
        public void Test()
        {
            //RunGenerator(6.0m);
            RunGenerator(7.0m);
            RunGenerator(8.0m);
            RunGenerator(9.0m);
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
                $"{netCoreVersionPath}\\System.Runtime.dll",
            };
            var references = referenceFiles.Select(x => MetadataReference.CreateFromFile(x)).ToArray();

            var compilation = CSharpCompilation.Create("ZerraSourceGenerationTestAssembly",
                [SyntaxFactory.ParseSyntaxTree(TypesToGenerateCode.Code)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var sourceGenerator = new ZerraIncrementalGenerator().AsSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

            var driverResult = driver.RunGenerators(compilation);
            var results = driverResult.GetRunResult();
            var result = results.Results.Single();

            var directory = DirectoryHelper.SolutionDirectory;
            var path = $"{directory}{Path.DirectorySeparatorChar}Framework{Path.DirectorySeparatorChar}Zerra{Path.DirectorySeparatorChar}Generated";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = $"{path}{Path.DirectorySeparatorChar}{directive}";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var source in result.GeneratedSources)
            {
                var wrapper = $$""""
#if {{directive}}

{{source.SourceText.ToString()}}

#endif
"""";
                var filePath = $"{path}{Path.DirectorySeparatorChar}{source.HintName}";
                File.WriteAllText(filePath, wrapper, source.SourceText.Encoding ?? Encoding.UTF8);
            }

            var sourceFiles = result.GeneratedSources.Select(x => x.HintName).ToHashSet();
            foreach (var file in Directory.GetFiles(path).Where(x => !sourceFiles.Contains(x.Split(Path.DirectorySeparatorChar).Last())))
                File.Delete(file);
        }
    }
}
