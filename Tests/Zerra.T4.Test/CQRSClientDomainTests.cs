// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.T4.Test
{
    public class CQRSClientDomainTests
    {
        private const string domainSource = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Zerra.CQRS;

            namespace TestApp
            {
                public enum WidgetKind { Small, Large }

                public class Part
                {
                    public int ID { get; set; }
                    public decimal? Weight { get; set; }
                }

                public class Widget
                {
                    public Guid ID { get; set; }
                    public string? Name { get; set; }
                    public WidgetKind Kind { get; set; }
                    public DateTime? Built { get; set; }
                    public List<Part> Parts { get; set; }
                    public int[] Numbers { get; set; }
                }

                public class WidgetSummary
                {
                    public int Count { get; set; }
                }

                public class Unused
                {
                    public int Value { get; set; }
                }

                public interface IWidgetQueryHandler : IQueryHandler
                {
                    Task<Widget> GetWidget(Guid id);
                    Task<List<Widget>> GetWidgets(string search, CancellationToken cancellationToken);
                    Task<bool> Exists(Guid id);
                }

                public class SaveWidgetCommand : ICommand
                {
                    public Widget Widget { get; set; }
                }

                public class CountWidgetsCommand : ICommand<WidgetSummary>
                {
                    public WidgetKind Kind { get; set; }
                }

                public class NextNumberCommand : ICommand<int>
                {
                }
            }
            """;

        [Fact]
        public void TypeScriptModels()
        {
            var output = GeneratorRunner.TypeScript(domainSource);

            Assert.StartsWith("import { Bus, ICommand } from \"./Bus\";", output);
            Assert.Contains("export class Widget {", output);
            Assert.Contains("    ID!: string;", output);
            Assert.Contains("    Name!: string | null;", output);
            //enums go over the wire by name
            Assert.Contains("    Kind!: string;", output);
            Assert.Contains("    Built!: Date | null;", output);
            Assert.Contains("    Parts!: Part[];", output);
            Assert.Contains("    Numbers!: number[];", output);
            Assert.Contains("    Parts: \"Part[]\",", output);
            //models reached through other models and command results
            Assert.Contains("export class Part {", output);
            Assert.Contains("    Weight!: number | null;", output);
            Assert.Contains("export class WidgetSummary {", output);
            Assert.Contains("    WidgetSummary: WidgetSummaryType,", output);
            Assert.DoesNotContain("Unused", output);
            Assert.DoesNotContain("export class WidgetKind", output);
        }

        [Fact]
        public void TypeScriptQueries()
        {
            var output = GeneratorRunner.TypeScript(domainSource);

            Assert.Contains("export class IWidgetQueryHandler {", output);
            Assert.Contains("    public static GetWidget(id: string): Promise<Widget> {", output);
            Assert.Contains("        return Bus.Call(\"TestApp.IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false);", output);
            //the server supplies the CancellationToken, the client holds its place with null
            Assert.Contains("    public static GetWidgets(search: string | null): Promise<Widget[]> {", output);
            Assert.Contains("        return Bus.Call(\"TestApp.IWidgetQueryHandler\", \"GetWidgets\", [search, null], WidgetType, true);", output);
            Assert.Contains("        return Bus.Call(\"TestApp.IWidgetQueryHandler\", \"Exists\", [id], null, false);", output);
        }

        [Fact]
        public void TypeScriptCommands()
        {
            var output = GeneratorRunner.TypeScript(domainSource);

            Assert.Contains("export class SaveWidgetCommand implements ICommand {", output);
            Assert.Contains("        self[\"CommandType\"] = \"TestApp.SaveWidgetCommand\";", output);
            Assert.Contains("        self[\"CommandWithResult\"] = false;", output);
            Assert.Contains("    Widget!: Widget | null;", output);

            Assert.Contains("        self[\"CommandType\"] = \"TestApp.CountWidgetsCommand\";\r\n        self[\"CommandWithResult\"] = true;\r\n        self[\"ResultType\"] = WidgetSummaryType;", output);
            //JavaScript result types have no model type
            Assert.Contains("        self[\"CommandType\"] = \"TestApp.NextNumberCommand\";\r\n        self[\"CommandWithResult\"] = true;\r\n        self[\"ResultType\"] = null;", output);
        }

        [Fact]
        public void JavaScriptModels()
        {
            var output = GeneratorRunner.JavaScript(domainSource);

            Assert.Contains("const WidgetType =\r\n{", output);
            Assert.Contains("    Kind: \"string\",", output);
            Assert.Contains("    Parts: \"Part[]\",", output);
            Assert.Contains("const PartType =", output);
            Assert.Contains("const WidgetSummaryType =", output);
            Assert.Contains("const ModelTypeDictionary =", output);
            Assert.DoesNotContain("Unused", output);
            Assert.DoesNotContain("export ", output);
        }

        [Fact]
        public void JavaScriptQueries()
        {
            var output = GeneratorRunner.JavaScript(domainSource);

            Assert.Contains("const IWidgetQueryHandler = {", output);
            Assert.Contains("    GetWidget: function(id, onComplete, onFail) {", output);
            Assert.Contains("        Bus.Call(\"TestApp.IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false, onComplete, onFail);", output);
            Assert.Contains("    GetWidgets: function(search, onComplete, onFail) {", output);
            Assert.Contains("        Bus.Call(\"TestApp.IWidgetQueryHandler\", \"GetWidgets\", [search, null], WidgetType, true, onComplete, onFail);", output);
        }

        [Fact]
        public void JavaScriptCommands()
        {
            var output = GeneratorRunner.JavaScript(domainSource);

            Assert.Contains("const SaveWidgetCommand = function(properties) {", output);
            Assert.Contains("    this.Widget = (properties === undefined || properties.Widget === undefined) ? null : properties.Widget;", output);
            Assert.Contains("    this.CommandType = \"TestApp.SaveWidgetCommand\";", output);
            Assert.Contains("    this.CommandType = \"TestApp.CountWidgetsCommand\";\r\n    this.CommandWithResult = true;\r\n    this.ResultType = WidgetSummaryType;", output);
            Assert.Contains("    this.CommandType = \"TestApp.NextNumberCommand\";\r\n    this.CommandWithResult = true;\r\n    this.ResultType = null;", output);
        }

        [Fact]
        public void FileScopedNamespace()
        {
            var output = GeneratorRunner.TypeScript("""
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp.Domain;

                public class Widget
                {
                    public int ID { get; set; }
                }

                public interface IWidgetQueryHandler : IQueryHandler
                {
                    Task<Widget> GetWidget(int id);
                }

                public class SaveWidgetCommand : ICommand
                {
                    public int ID { get; set; }
                }
                """);

            Assert.Contains("        return Bus.Call(\"TestApp.Domain.IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false);", output);
            Assert.Contains("export class Widget {", output);
            Assert.Contains("        self[\"CommandType\"] = \"TestApp.Domain.SaveWidgetCommand\";", output);
        }

        //the server finds the handler and command by full name, which includes the outer namespace
        [Fact]
        public void NestedNamespace()
        {
            var output = GeneratorRunner.JavaScript("""
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    namespace Domain
                    {
                        public interface IWidgetQueryHandler : IQueryHandler
                        {
                            Task<bool> Exists(int id);
                        }

                        public class SaveWidgetCommand : ICommand
                        {
                            public int ID { get; set; }
                        }
                    }
                }
                """);

            Assert.Contains("        Bus.Call(\"TestApp.Domain.IWidgetQueryHandler\", \"Exists\", [id], null, false, onComplete, onFail);", output);
            Assert.Contains("    this.CommandType = \"TestApp.Domain.SaveWidgetCommand\";", output);
        }

        [Fact]
        public void ModelsThroughGenericArguments()
        {
            var output = GeneratorRunner.TypeScript("""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.CQRS;
                using TestApp.Models;

                namespace TestApp.Models
                {
                    public class Tag
                    {
                        public string Name { get; set; }
                    }
                    public class Note
                    {
                        public string Text { get; set; }
                    }
                    public class Widget
                    {
                        public Dictionary<string, Tag> Tags { get; set; }
                        public TestApp.Models.Note Note { get; set; }
                    }
                }

                namespace TestApp
                {
                    public interface IWidgetQueryHandler : IQueryHandler
                    {
                        Task<Widget> GetWidget(int id);
                    }
                }
                """);

            //Tag is only reached through the second generic argument and Note through a qualified name
            Assert.Contains("export class Tag {", output);
            Assert.Contains("export class Note {", output);
            Assert.Contains("    Note!: Note | null;", output);
        }

        [Fact]
        public void Records()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public record Widget(int ID, string Name, List<Part> Parts);
                    public record struct Part(decimal Weight);
                    public record WidgetResult(bool Saved);

                    public interface IWidgetQueryHandler : IQueryHandler
                    {
                        Task<Widget> GetWidget(int id);
                    }

                    public record SaveWidgetCommand(Widget Widget) : ICommand<WidgetResult>;
                }
                """;

            var typeScript = GeneratorRunner.TypeScript(source);
            Assert.Contains("export class Widget {\r\n    ID!: number;\r\n    Name!: string | null;\r\n    Parts!: Part[];\r\n}", typeScript);
            Assert.Contains("export class Part {\r\n    Weight!: number;\r\n}", typeScript);
            Assert.Contains("        return Bus.Call(\"TestApp.IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false);", typeScript);
            Assert.Contains("export class SaveWidgetCommand implements ICommand {", typeScript);
            Assert.Contains("        self[\"ResultType\"] = WidgetResultType;", typeScript);
            Assert.Contains("    Widget!: Widget | null;", typeScript);

            var javaScript = GeneratorRunner.JavaScript(source);
            Assert.Contains("const WidgetType =\r\n{\r\n    ID: \"number\",\r\n    Name: \"string\",\r\n    Parts: \"Part[]\",\r\n}", javaScript);
            Assert.Contains("    this.Widget = (properties === undefined || properties.Widget === undefined) ? null : properties.Widget;", javaScript);
        }

        //the real demo domain, uses project files for implicit usings and spans multiple folders
        [Fact]
        public void PetsDomain()
        {
            var directory = Path.Combine(GeneratorRunner.SolutionDirectory, "Demo", "Pets", "Pets.Domain");

            var typeScript = CQRSClientDomain.GenerateTypeScript(directory);
            Assert.Contains("export class IPetsQueryHandler {", typeScript);
            Assert.Contains("        return Bus.Call(\"Pets.Domain.IPetsQueryHandler\", \"GetPets\", [], PetModelType, true);", typeScript);
            Assert.Contains("        return Bus.Call(\"Pets.Domain.IPetsQueryHandler\", \"GetPet\", [id], PetModelType, false);", typeScript);
            Assert.Contains("export class PetModel {", typeScript);
            Assert.Contains("    LastEaten!: Date | null;", typeScript);
            Assert.Contains("        self[\"CommandType\"] = \"Pets.Domain.Commands.AddPetCommand\";", typeScript);

            var javaScript = CQRSClientDomain.GenerateJavaScript(directory);
            Assert.Contains("const IPetsQueryHandler = {", javaScript);
            Assert.Contains("const PetModelType =", javaScript);
            Assert.Contains("    this.CommandType = \"Pets.Domain.Commands.AddPetCommand\";", javaScript);
        }
    }
}
