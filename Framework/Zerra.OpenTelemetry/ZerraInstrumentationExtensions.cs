// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Zerra.OpenTelemetry
{
    /// <summary>
    /// Adds Zerra's spans and metrics to OpenTelemetry.
    /// </summary>
    public static class ZerraInstrumentationExtensions
    {
        /// <summary>
        /// Collects the spans <see cref="OpenTelemetryBusLogger"/> records.
        /// </summary>
        /// <param name="builder">The tracer provider builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static TracerProviderBuilder AddZerraInstrumentation(this TracerProviderBuilder builder)
            => builder.AddSource(OpenTelemetryBusLogger.SourceName);

        /// <summary>
        /// Collects the durations <see cref="OpenTelemetryBusLogger"/> records.
        /// </summary>
        /// <param name="builder">The meter provider builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static MeterProviderBuilder AddZerraInstrumentation(this MeterProviderBuilder builder)
            => builder.AddMeter(OpenTelemetryBusLogger.SourceName);
    }
}
