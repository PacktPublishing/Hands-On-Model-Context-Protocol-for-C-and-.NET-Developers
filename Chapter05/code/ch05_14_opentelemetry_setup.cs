// Chapter 5 — Section 5.3.5
// Full OpenTelemetry registration in Program.cs.
// AddSource("*") captures spans from any ActivitySource, including the MCP SDK's own spans.
// UseOtlpExporter() sends telemetry to the configured OTLP endpoint (e.g. Grafana, Jaeger).
// Chapter 12 walks through connecting these traces to a live dashboard.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("*")                          // captures SDK + custom spans
        .AddAspNetCoreInstrumentation()          // HTTP request spans
        .AddHttpClientInstrumentation())         // outbound HTTP client spans
    .WithMetrics(metrics => metrics
        .AddMeter("*")                           // captures SDK + custom meters
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()                               // structured log export via OTLP
    .UseOtlpExporter();                          // configure endpoint via OTEL_EXPORTER_OTLP_ENDPOINT
