using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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

            var test = @"using System;

namespace ZerraDemo.Domain.Pets.Models
{
    public class PetModel
    {
        public Guid ID { get; set; }
        public string? Name { get; set; }
        public string? Breed { get; set; }
        public string? Species { get; set; }

        public DateTime? LastEaten { get; set; }
        public int? AmountEaten { get; set; }
        public DateTime? LastPooped { get; set; }
    }
}

";

            var compilation = CSharpCompilation.Create("TestProject",
                [SyntaxFactory.ParseSyntaxTree(test)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            var sourceGenerator = new ZerraIncrementalGenerator().AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
        }
    }
}
