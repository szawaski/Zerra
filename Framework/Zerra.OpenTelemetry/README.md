# Zerra.OpenTelemetry

OpenTelemetry tracing and metrics for [Zerra](https://www.nuget.org/packages/Zerra) queries, commands, and events.

## Installation

```bash
dotnet add package Zerra.OpenTelemetry
```

## Setup

Pass an `OpenTelemetryBusLogger` to `Bus.New`, and add Zerra to OpenTelemetry with `AddZerraInstrumentation`:

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Zerra.CQRS;
using Zerra.OpenTelemetry;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddZerraInstrumentation()
    .AddOtlpExporter()
    .Build();
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddZerraInstrumentation()
    .AddOtlpExporter()
    .Build();

var bus = Bus.New("OrderService", log, new OpenTelemetryBusLogger());
```

In ASP.NET Core, add it with the other instrumentation:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddZerraInstrumentation().AddOtlpExporter())
    .WithMetrics(metrics => metrics.AddZerraInstrumentation().AddOtlpExporter());
```

To keep an existing bus logger, pass it in: `new OpenTelemetryBusLogger(new ConsoleBusLogger())`.

## What Is Recorded

- A span for every query, command, and event, on the side that sends it and on the side that handles it. The handler's span continues the sender's trace, across services and every transport.
- A `zerra.bus.duration` histogram, in seconds, by interface and method or message type, by whether it was sent or handled, and by `error.type` for failures.
- Failures mark the span as an error with the exception's type and message.

Arguments and message contents are never recorded.

A service that uses Zerra without this package still passes the trace on, so the services before and after it stay in the same trace.
