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

            var test = @"

using System;

namespace ZerraDemo.Domain.Weather.Constants
{
    [Flags]
    public enum WeatherType
    {
        Sunny = 0,
        OhioGraySkies = 1,
        Cloudy = 2,
        Windy = 4,
        Rain = 8,
        Snow = 16,
        Hail = 32,
        Tornado = 64,
        Hurricane = 128,
        Asteroid = 256,
        Sharks = 512
    }

    public interface IWeatherQueryProvider
    {
        WeatherModel? GetWeather();
        Task<Stream> TestStreams();
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
