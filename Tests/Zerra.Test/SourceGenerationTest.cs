using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Zerra.CQRS;
using Zerra.SourceGeneration;

namespace Zerra.Test
{
    [TestClass]
    [ServiceSecureAttribute("beep", "bop")]
    public class SourceGenerationTest
    {
        [TestMethod]
        public void Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).ToArray();
            var references = assemblies.Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray();

           
            var compilation = CSharpCompilation.Create("TestProject",
                [SyntaxFactory.ParseSyntaxTree("[Zerra.CQRS.ServiceSecure(\"beep\",\"bop\")] public class TestGen { }")],
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
