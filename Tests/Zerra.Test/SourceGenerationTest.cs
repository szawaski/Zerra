using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Zerra.SourceGeneration;

namespace Zerra.Test
{
    [TestClass]
    public class SourceGenerationTest
    {
        [TestMethod]
        public void Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).ToArray();
            var references = assemblies.Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray();

            var compilation = CSharpCompilation.Create("TestProject",
                [SyntaxFactory.ParseSyntaxTree("public class TestGen<T> : System.Collections.Generic.List<T> where T : class, new() { }")],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            var sourceGenerator = new TypeDetailSourceGenerator().AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
        }
    }
}
