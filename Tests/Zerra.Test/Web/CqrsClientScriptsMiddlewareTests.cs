// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Xunit;
using Zerra.CQRS;
using Zerra.Serialization.Json;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class CqrsClientScriptsMiddlewareTests
    {
        [Fact]
        public async Task TypeScriptModels()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.StartsWith("import { Bus, type ICommand } from \"./Bus\";", output);
            Assert.Contains("export class Widget {", output);
            Assert.Contains("    ID!: string;", output);
            Assert.Contains("    Name!: string | null;", output);
            Assert.Contains("    Kind!: string;", output);
            Assert.Contains("    Built!: Date | null;", output);
            Assert.Contains("    Parts!: Part[];", output);
            Assert.Contains("    Numbers!: number[];", output);
            Assert.Contains("    Parts: \"Part[]\",", output);
            Assert.Contains("export class Part {", output);
            Assert.Contains("    Weight!: number | null;", output);
            Assert.Contains("export class WidgetSummary {", output);
            Assert.Contains("    WidgetSummary: WidgetSummaryType,", output);
            Assert.DoesNotContain("Unused", output);
            Assert.DoesNotContain("export class WidgetKind", output);
            Assert.DoesNotContain("CancellationToken", output);
            //the members every handler has aren't queries
            Assert.DoesNotContain("Initialize", output);
            Assert.DoesNotContain("BusContext", output);
            Assert.DoesNotContain("get_", output);
        }

        [Fact]
        public async Task TypeScriptQueries()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.Contains("export class IWidgetQueryHandler {", output);
            Assert.Contains("    public static GetWidget(id: string): Promise<Widget | null> {", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false);", output);
            Assert.Contains("    public static GetWidgets(search: string | null): Promise<Widget[]> {", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"GetWidgets\", [search, null], WidgetType, true);", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"Exists\", [id], null, false);", output);
            Assert.Contains("    public static Touch(id: string): Promise<void> {", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"Touch\", [id], null, false);", output);
            Assert.Contains("    public static Download(id: string): Promise<Blob | null> {", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"Download\", [id], \"Blob\", false);", output);
            Assert.Contains("    public static Upload(stream: Blob | null): Promise<number> {", output);
            Assert.Contains("    public static Count(): Promise<number> {", output);
        }

        [Fact]
        public async Task TypeScriptCommands()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.Contains("export class SaveWidgetCommand implements ICommand {", output);
            Assert.Contains("        self[\"CommandType\"] = \"SaveWidgetCommand\";", output);
            Assert.Contains("        self[\"CommandWithResult\"] = false;", output);
            Assert.Contains("    Widget!: Widget | null;", output);

            Assert.Contains($"        self[\"CommandType\"] = \"CountWidgetsCommand\";{Environment.NewLine}        self[\"CommandWithResult\"] = true;{Environment.NewLine}        self[\"ResultType\"] = WidgetSummaryType;", output);
            Assert.Contains($"        self[\"CommandType\"] = \"NextNumberCommand\";{Environment.NewLine}        self[\"CommandWithResult\"] = true;{Environment.NewLine}        self[\"ResultType\"] = null;", output);
        }

        [Fact]
        public async Task JavaScriptModels()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/JavaScriptModels.js");

            Assert.Contains($"const WidgetType ={Environment.NewLine}{{", output);
            Assert.Contains("    Kind: \"string\",", output);
            Assert.Contains("    Parts: \"Part[]\",", output);
            Assert.Contains("const PartType =", output);
            Assert.Contains("const WidgetSummaryType =", output);
            Assert.Contains("Bus.addModels({", output);
            Assert.Contains("    Widget: WidgetType,", output);
            Assert.DoesNotContain("Unused", output);
            Assert.DoesNotContain("export ", output);
        }

        [Fact]
        public async Task JavaScriptQueries()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/JavaScriptModels.js");

            Assert.Contains("const IWidgetQueryHandler = {", output);
            Assert.Contains("    GetWidget: function(id, onComplete, onFail) {", output);
            Assert.Contains("        Bus.call(\"IWidgetQueryHandler\", \"GetWidget\", [id], WidgetType, false, onComplete, onFail);", output);
            Assert.Contains("    GetWidgets: function(search, onComplete, onFail) {", output);
            Assert.Contains("        Bus.call(\"IWidgetQueryHandler\", \"GetWidgets\", [search, null], WidgetType, true, onComplete, onFail);", output);
            Assert.Contains("        Bus.call(\"IWidgetQueryHandler\", \"Download\", [id], \"Blob\", false, onComplete, onFail);", output);
        }

        [Fact]
        public async Task JavaScriptCommands()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/JavaScriptModels.js");

            Assert.Contains("const SaveWidgetCommand = function(properties) {", output);
            Assert.Contains("    this.Widget = (properties === undefined || properties.Widget === undefined) ? null : properties.Widget;", output);
            Assert.Contains("    this.CommandType = \"SaveWidgetCommand\";", output);
            Assert.Contains($"    this.CommandType = \"CountWidgetsCommand\";{Environment.NewLine}    this.CommandWithResult = true;{Environment.NewLine}    this.ResultType = WidgetSummaryType;", output);
            Assert.Contains($"    this.CommandType = \"NextNumberCommand\";{Environment.NewLine}    this.CommandWithResult = true;{Environment.NewLine}    this.ResultType = null;", output);
        }

        [Fact]
        public async Task EventsAreLeftOut()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.DoesNotContain("WidgetSavedEvent", output);
            Assert.DoesNotContain("IWidgetCommandHandler", output);
            Assert.DoesNotContain("IWidgetEventHandler", output);
        }

        [Fact]
        public async Task ModelsThroughGenericArguments()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            //Tag is only reached through a dictionary, which the client leaves as it is
            Assert.Contains("export class Tag {", output);
            Assert.Contains("    Tags!: any;", output);
            Assert.Contains("export class PagedResultOfWidget {", output);
            Assert.Contains("    Items!: Widget[];", output);
            Assert.Contains("    public static Page(page: number): Promise<PagedResultOfWidget | null> {", output);
            Assert.Contains("        return Bus.call(\"IWidgetQueryHandler\", \"Page\", [page], PagedResultOfWidgetType, false);", output);
        }

        [Fact]
        public async Task Records()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.Contains($"export class Note {{{Environment.NewLine}    ID!: number;{Environment.NewLine}    Text!: string | null;{Environment.NewLine}    Size!: NoteSize | null;{Environment.NewLine}}}", output);
            Assert.Contains($"export class NoteSize {{{Environment.NewLine}    Length!: number;{Environment.NewLine}}}", output);
            Assert.Contains("export class SaveNoteCommand implements ICommand {", output);
            Assert.Contains("    Note!: Note | null;", output);
        }

        [Fact]
        public async Task SpecialTypes()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            //JSON writes bytes as base64 and a TimeSpan as a duration
            Assert.Contains("    Data!: string | null;", output);
            Assert.Contains("    Took!: string;", output);
            Assert.Contains("    Started!: Date;", output);
            Assert.Contains("    Code!: string;", output);
            Assert.Contains("    Values!: any;", output);
            Assert.Contains("    Thing!: any;", output);
        }

        [Fact]
        public async Task JsonNamesAndIgnores()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/TypeScriptModels.ts");

            Assert.Contains($"export class Renamed {{{Environment.NewLine}    shown!: number;{Environment.NewLine}    Kept!: number;{Environment.NewLine}}}", output);
            Assert.DoesNotContain("Hidden", output);
            Assert.DoesNotContain("AlsoHidden", output);
        }

        [Fact]
        public async Task SameName_Throws()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<ISameNameQueryHandler>(new SameNameHandler());
            var middleware = new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus);
            var context = CreateContext("GET", "/CQRSClient/TypeScriptModels.ts");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));
            Assert.Contains("are both named Shared", exception.Message);
        }

        [Fact]
        public async Task CommandAsModel()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<INestedCommandHandler>(new NestedCommandHandler());

            var javaScript = await GetScript(bus, "/CQRSClient/JavaScriptModels.js");
            Assert.Contains($"const SaveLineCommandType ={Environment.NewLine}{{{Environment.NewLine}    Text: \"string\",", javaScript);
            Assert.Contains("    SaveLineCommand: SaveLineCommandType,", javaScript);
            Assert.Contains("const SaveLineCommand = function(properties) {", javaScript);
            Assert.Contains("const SaveLinesCommand = function(properties) {", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            //declared once, by its command class
            Assert.Equal(2, typeScript.Split("export class SaveLineCommand ").Length);
            Assert.Contains("export class SaveLineCommand implements ICommand {", typeScript);
            Assert.Contains("    Lines!: SaveLineCommand[];", typeScript);
            Assert.Contains("const SaveLineCommandType =", typeScript);
        }

        [Fact]
        public async Task MembersNotWritten()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IEdgeQueryHandler>(new EdgeHandler());

            //the server reads Incoming but never writes it, so nameless JSON has no value for it
            var javaScript = await GetScript(bus, "/CQRSClient/JavaScriptModels.js");
            Assert.Contains($"const IgnoredType ={Environment.NewLine}{{{Environment.NewLine}    Kept: \"number\",{Environment.NewLine}}}", javaScript);
            Assert.Equal("[1]", JsonSerializer.Serialize(new Ignored { Kept = 1, Incoming = 2, NotWritten = 3 }, new JsonSerializerOptions() { Nameless = true }));

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            Assert.Contains($"export class Ignored {{{Environment.NewLine}    Kept!: number;{Environment.NewLine}    Incoming!: number;{Environment.NewLine}}}", typeScript);
            Assert.Contains($"const IgnoredType ={Environment.NewLine}{{{Environment.NewLine}    Kept: \"number\",{Environment.NewLine}}}", typeScript);
        }

        [Fact]
        public async Task DateResults()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IEdgeQueryHandler>(new EdgeHandler());
            bus.AddHandler<IEdgeCommandHandler>(new EdgeHandler());

            var javaScript = await GetScript(bus, "/CQRSClient/JavaScriptModels.js");
            Assert.Contains("        Bus.call(\"IEdgeQueryHandler\", \"GetTime\", [], \"Date\", false, onComplete, onFail);", javaScript);
            Assert.Contains("        Bus.call(\"IEdgeQueryHandler\", \"GetTimes\", [], \"Date\", true, onComplete, onFail);", javaScript);
            Assert.Contains($"    this.CommandType = \"TimeCommand\";{Environment.NewLine}    this.CommandWithResult = true;{Environment.NewLine}    this.ResultType = \"Date\";", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            Assert.Contains("        return Bus.call(\"IEdgeQueryHandler\", \"GetTime\", [], \"Date\", false);", typeScript);
            Assert.Contains("        self[\"ResultType\"] = \"Date\";", typeScript);
        }

        [Fact]
        public async Task ArrayTypeArgument()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IEdgeQueryHandler>(new EdgeHandler());

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            Assert.Contains("export class PageOfArrayOfIgnored {", typeScript);
            Assert.Contains("    Items!: Ignored[];", typeScript);
            Assert.Contains("    PageOfArrayOfIgnored: PageOfArrayOfIgnoredType,", typeScript);
            Assert.Contains("    public static GetPage(): Promise<PageOfArrayOfIgnored | null> {", typeScript);
            Assert.DoesNotContain("[]Type", typeScript);
        }

        [Fact]
        public async Task ReservedParameterNames()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IEdgeQueryHandler>(new EdgeHandler());

            var javaScript = await GetScript(bus, "/CQRSClient/JavaScriptModels.js");
            Assert.Contains("    Remove: function(delete_, onComplete_, onComplete, onFail) {", javaScript);
            Assert.Contains("        Bus.call(\"IEdgeQueryHandler\", \"Remove\", [delete_, onComplete_], null, false, onComplete, onFail);", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            Assert.Contains("    public static Remove(delete_: number, onComplete_: number): Promise<number> {", typeScript);
        }

        [Fact]
        public async Task QuotedMemberNames()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IEdgeCommandHandler>(new EdgeHandler());

            var javaScript = await GetScript(bus, "/CQRSClient/JavaScriptModels.js");
            Assert.Contains("    this[\"time-zone\"] = (properties === undefined || properties[\"time-zone\"] === undefined) ? null : properties[\"time-zone\"];", javaScript);
            Assert.Contains("    this.Offset = (properties === undefined || properties.Offset === undefined) ? null : properties.Offset;", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/TypeScriptModels.ts");
            Assert.Contains("    \"time-zone\"!: string | null;", typeScript);
        }

        [Fact]
        public async Task ReservedName_Throws()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<IReservedNameQueryHandler>(new ReservedNameHandler());
            var middleware = new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(CreateContext("GET", "/CQRSClient/JavaScriptModels.js")));
            Assert.Contains("is named Date, which the client scripts already use", exception.Message);
        }

        [Fact]
        public async Task TypeMapName_Throws()
        {
            var bus = Bus.New("test", null, null, null);
            bus.AddHandler<ITypeMapNameQueryHandler>(new TypeMapNameHandler());
            var middleware = new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(CreateContext("GET", "/CQRSClient/TypeScriptModels.ts")));
            Assert.Contains("is named OrderType, which the client scripts use for the model", exception.Message);
        }

        [Fact]
        public async Task CustomRoute()
        {
            var bus = WidgetBus();
            var output = await GetScript(bus, "/scripts/JavaScriptModels.js", "/scripts/");
            Assert.Contains("const IWidgetQueryHandler = {", output);

            //the path doesn't care about case, like the rest of ASP.NET Core
            output = await GetScript(bus, "/SCRIPTS/javascriptmodels.js", "/scripts");
            Assert.Contains("const IWidgetQueryHandler = {", output);
        }

        [Fact]
        public async Task OtherRequests_GoToNext()
        {
            var bus = WidgetBus();
            foreach (var (method, path) in new[] { ("GET", "/CQRSClient"), ("GET", "/CQRSClient/Other.js"), ("GET", "/CQRS"), ("POST", "/CQRSClient/JavaScriptModels.js") })
            {
                var nextCalled = false;
                var middleware = new CqrsClientScriptsMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, bus);
                var context = CreateContext(method, path);

                await middleware.Invoke(context);

                Assert.True(nextCalled, $"{method} {path}");
                Assert.Equal(0, context.Response.Body.Length);
            }
        }

        [Fact]
        public async Task ContentTypes()
        {
            var bus = WidgetBus();
            var middleware = new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus);

            var context = CreateContext("GET", "/CQRSClient/JavaScriptModels.js");
            await middleware.Invoke(context);
            Assert.Equal("text/javascript; charset=utf-8", context.Response.ContentType);

            context = CreateContext("GET", "/CQRSClient/TypeScriptModels.ts");
            await middleware.Invoke(context);
            Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);

            context = CreateContext("GET", "/CQRSClient/Bus.js");
            await middleware.Invoke(context);
            Assert.Equal("text/javascript; charset=utf-8", context.Response.ContentType);

            context = CreateContext("GET", "/CQRSClient/Bus.ts");
            await middleware.Invoke(context);
            Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task BusScripts()
        {
            var bus = WidgetBus();

            var javaScript = await GetScript(bus, "/CQRSClient/Bus.js");
            Assert.Contains("const Bus = {", javaScript);
            Assert.Contains("_routes: { \"Gateway\": \"/CQRS\" },", javaScript);
            Assert.Contains("setRoute: function (name, url) {", javaScript);
            Assert.Contains("onFail: function (callback) {", javaScript);
            Assert.Contains("addModels: function (models) {", javaScript);
            Assert.DoesNotContain("﻿", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/Bus.ts");
            Assert.Contains("export class Bus {", typeScript);
            Assert.Contains("private static _routes: { [index: string]: string } = { \"Gateway\": \"/CQRS\" };", typeScript);
            Assert.Contains("public static setRoute(name: string, url: string): void {", typeScript);
            Assert.Contains("public static addModels(models: { [index: string]: any }): void {", typeScript);
        }

        [Fact]
        public async Task GatewayRoute()
        {
            var bus = WidgetBus();

            var javaScript = await GetScript(bus, "/CQRSClient/Bus.js", gatewayRoute: "https://api.example.com/gateway");
            Assert.Contains("_routes: { \"Gateway\": \"https://api.example.com/gateway\" },", javaScript);
            Assert.DoesNotContain("\"/CQRS\"", javaScript);

            var typeScript = await GetScript(bus, "/CQRSClient/Bus.ts", gatewayRoute: "/api/cqrs");
            Assert.Contains("= { \"Gateway\": \"/api/cqrs\" };", typeScript);

            _ = Assert.Throws<ArgumentException>(() => new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus, "/CQRSClient", "/CQRS\"; alert(1); \""));
            _ = Assert.Throws<ArgumentException>(() => new CqrsClientScriptsMiddleware(_ => Task.CompletedTask, bus, "/CQRSClient", " "));
        }

        //the type maps are in the order the serializer writes members, nameless JSON is read by position
        [Fact]
        public async Task MemberOrderMatchesNamelessJson()
        {
            var output = await GetScript(WidgetBus(), "/CQRSClient/JavaScriptModels.js");

            var start = output.IndexOf("const PartType =");
            var map = output.Substring(start, output.IndexOf('}', start) - start);
            Assert.True(map.IndexOf("ID:") < map.IndexOf("Weight:"));

            var json = JsonSerializer.Serialize(new Part { ID = 5, Weight = 2.5m }, new JsonSerializerOptions() { Nameless = true });
            Assert.Equal("[5,2.5]", json);
        }

        private static IBus WidgetBus()
        {
            var bus = Bus.New("test", null, null, null);
            var handler = new WidgetHandler();
            bus.AddHandler<IWidgetQueryHandler>(handler);
            bus.AddHandler<IWidgetCommandHandler>(handler);
            bus.AddHandler<IWidgetEventHandler>(handler);
            return bus;
        }

        private static async Task<string> GetScript(IBus bus, string path, string route = "/CQRSClient", string gatewayRoute = "/CQRS")
        {
            var middleware = new CqrsClientScriptsMiddleware(_ => throw new InvalidOperationException("next called"), bus, route, gatewayRoute);
            var context = CreateContext("GET", path);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }

        private static DefaultHttpContext CreateContext(string method, string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        public enum WidgetKind { Small, Large }

        public sealed class Part
        {
            public int ID { get; set; }
            public decimal? Weight { get; set; }
        }

        public sealed class Tag
        {
            public string? Name { get; set; }
        }

        public sealed class Widget
        {
            public Guid ID { get; set; }
            public string? Name { get; set; }
            public WidgetKind Kind { get; set; }
            public DateTime? Built { get; set; }
            public List<Part>? Parts { get; set; }
            public int[]? Numbers { get; set; }
            public Dictionary<string, Tag>? Tags { get; set; }
        }

        public sealed class PagedResult<T>
        {
            public List<T>? Items { get; set; }
            public int Total { get; set; }
        }

        public sealed class WidgetSummary
        {
            public int Count { get; set; }
        }

        public sealed class Unused
        {
            public int Value { get; set; }
        }

        public sealed record Note(int ID, string Text, NoteSize Size);
        public sealed record NoteSize(int Length);

        public sealed class Special
        {
            public byte[]? Data { get; set; }
            public TimeSpan Took { get; set; }
            public DateTimeOffset Started { get; set; }
            public char Code { get; set; }
            public Dictionary<int, string>? Values { get; set; }
            public object? Thing { get; set; }
        }

        public sealed class Renamed
        {
            [JsonPropertyName("shown")]
            public int Shown { get; set; }
            [JsonIgnore]
            public int Hidden { get; set; }
            [System.Text.Json.Serialization.JsonIgnore]
            public int AlsoHidden { get; set; }
            public int Kept { get; set; }
        }

        public interface IWidgetQueryHandler : IQueryHandler
        {
            Task<Widget> GetWidget(Guid id);
            Task<List<Widget>> GetWidgets(string search, CancellationToken cancellationToken);
            Task<bool> Exists(Guid id);
            Task Touch(Guid id);
            Task<Stream> Download(Guid id);
            Task<int> Upload(Stream stream);
            int Count();
            Task<PagedResult<Widget>> Page(int page);
            Task<Special> GetSpecial();
            Task<Renamed> GetRenamed();
        }

        public sealed class SaveWidgetCommand : ICommand
        {
            public Widget? Widget { get; set; }
        }

        public sealed class CountWidgetsCommand : ICommand<WidgetSummary>
        {
            public WidgetKind Kind { get; set; }
        }

        public sealed class NextNumberCommand : ICommand<int>
        {
        }

        public sealed record SaveNoteCommand(Note Note) : ICommand;

        public sealed class WidgetSavedEvent : IEvent
        {
            public Guid ID { get; set; }
        }

        public interface IWidgetCommandHandler : ICommandHandler<SaveWidgetCommand>, ICommandHandler<CountWidgetsCommand, WidgetSummary>, ICommandHandler<NextNumberCommand, int>, ICommandHandler<SaveNoteCommand> { }
        public interface IWidgetEventHandler : IEventHandler<WidgetSavedEvent> { }

        public sealed class WidgetHandler : BaseHandler, IWidgetQueryHandler, IWidgetCommandHandler, IWidgetEventHandler
        {
            public Task<Widget> GetWidget(Guid id) => throw new NotImplementedException();
            public Task<List<Widget>> GetWidgets(string search, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<bool> Exists(Guid id) => throw new NotImplementedException();
            public Task Touch(Guid id) => throw new NotImplementedException();
            public Task<Stream> Download(Guid id) => throw new NotImplementedException();
            public Task<int> Upload(Stream stream) => throw new NotImplementedException();
            public int Count() => throw new NotImplementedException();
            public Task<PagedResult<Widget>> Page(int page) => throw new NotImplementedException();
            public Task<Special> GetSpecial() => throw new NotImplementedException();
            public Task<Renamed> GetRenamed() => throw new NotImplementedException();
            public Task Handle(SaveWidgetCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<WidgetSummary> Handle(CountWidgetsCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<int> Handle(NextNumberCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task Handle(SaveNoteCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task Handle(WidgetSavedEvent @event) => throw new NotImplementedException();
        }

        public sealed class SaveLineCommand : ICommand
        {
            public string? Text { get; set; }
        }

        public sealed class SaveLinesCommand : ICommand
        {
            public SaveLineCommand[]? Lines { get; set; }
        }

        public interface INestedCommandHandler : ICommandHandler<SaveLineCommand>, ICommandHandler<SaveLinesCommand> { }

        public sealed class NestedCommandHandler : BaseHandler, INestedCommandHandler
        {
            public Task Handle(SaveLineCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task Handle(SaveLinesCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        public static class First
        {
            public sealed class Shared
            {
                public int Value { get; set; }
            }
        }

        public static class Second
        {
            public sealed class Shared
            {
                public int Value { get; set; }
            }
        }

        public interface ISameNameQueryHandler : IQueryHandler
        {
            Task<First.Shared> GetFirst();
            Task<Second.Shared> GetSecond();
        }

        public sealed class SameNameHandler : BaseHandler, ISameNameQueryHandler
        {
            public Task<First.Shared> GetFirst() => throw new NotImplementedException();
            public Task<Second.Shared> GetSecond() => throw new NotImplementedException();
        }

        public sealed class Ignored
        {
            public int Kept { get; set; }
            [Zerra.Serialization.Json.JsonIgnore(Zerra.Serialization.Json.JsonIgnoreCondition.WhenWriting)]
            public int Incoming { get; set; }
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWriting)]
            public int NotWritten { get; set; }
        }

        public sealed class Page<T>
        {
            public T? Items { get; set; }
        }

        public sealed class TimeCommand : ICommand<DateTime>
        {
            [JsonPropertyName("time-zone")]
            public string? TimeZone { get; set; }
            public int Offset { get; set; }
        }

        public interface IEdgeQueryHandler : IQueryHandler
        {
            Task<Ignored> GetIgnored();
            Task<DateTime> GetTime();
            Task<DateTime[]> GetTimes();
            Task<Page<Ignored[]>> GetPage();
            Task<int> Remove(int delete, int onComplete);
        }

        public interface IEdgeCommandHandler : ICommandHandler<TimeCommand, DateTime> { }

        public sealed class EdgeHandler : BaseHandler, IEdgeQueryHandler, IEdgeCommandHandler
        {
            public Task<Ignored> GetIgnored() => throw new NotImplementedException();
            public Task<DateTime> GetTime() => throw new NotImplementedException();
            public Task<DateTime[]> GetTimes() => throw new NotImplementedException();
            public Task<Page<Ignored[]>> GetPage() => throw new NotImplementedException();
            public Task<int> Remove(int delete, int onComplete) => throw new NotImplementedException();
            public Task<DateTime> Handle(TimeCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        public sealed class Date
        {
            public int Day { get; set; }
        }

        public interface IReservedNameQueryHandler : IQueryHandler
        {
            Task<Date> GetDate();
        }

        public sealed class ReservedNameHandler : BaseHandler, IReservedNameQueryHandler
        {
            public Task<Date> GetDate() => throw new NotImplementedException();
        }

        public sealed class Order
        {
            public int ID { get; set; }
        }

        public sealed class OrderType
        {
            public string? Name { get; set; }
        }

        public interface ITypeMapNameQueryHandler : IQueryHandler
        {
            Task<Order> GetOrder();
            Task<OrderType> GetOrderType();
        }

        public sealed class TypeMapNameHandler : BaseHandler, ITypeMapNameQueryHandler
        {
            public Task<Order> GetOrder() => throw new NotImplementedException();
            public Task<OrderType> GetOrderType() => throw new NotImplementedException();
        }
    }
}
