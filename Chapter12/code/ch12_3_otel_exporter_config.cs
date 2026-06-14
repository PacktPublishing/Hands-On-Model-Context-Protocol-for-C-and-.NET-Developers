// Chapter 12 — Section 12.1.3
// OpenTelemetry configuration subscribing to both the SDK's built-in
// "Experimental.ModelContextProtocol" source/meter and the application-level
// "TravelBooking.Flights" source/meter.
// UseOtlpExporter() configures OTLP export for traces, metrics, AND logs
// in a single call — set OTEL_EXPORTER_OTLP_ENDPOINT to the collector URL.
// AddPrometheusExporter() exposes a scrape endpoint at /metrics for Prometheus.
// AddAzureMonitorTraceExporter / AddAzureMonitorMetricExporter send to App Insights.

using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var resource = ResourceBuilder.CreateDefault()
    .AddService("flights-mcp-server", serviceVersion: "1.0.0");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resource)
        // SDK built-in: spans for every MCP tool call, resource fetch, and notification.
        .AddSource("Experimental.ModelContextProtocol")
        // Application-level: spans around downstream airline API calls.
        .AddSource("TravelBooking.Flights")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorTraceExporter(o =>
            o.ConnectionString = builder.Configuration["AppInsights:ConnectionString"])
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]!)))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resource)
        // SDK built-in: mcp.server.operation.duration, mcp.client.operation.duration,
        //               mcp.server.session.duration, mcp.client.session.duration
        .AddMeter("Experimental.ModelContextProtocol")
        // Application-level: mcp.tool.invocations, mcp.tool.duration_ms, mcp.connections.active
        .AddMeter("TravelBooking.Flights")
        .AddAspNetCoreInstrumentation()
        .AddAzureMonitorMetricExporter(o =>
            o.ConnectionString = builder.Configuration["AppInsights:ConnectionString"])
        .AddPrometheusExporter())
    .WithLogging()
    .UseOtlpExporter();

var app = builder.Build();

// Exposes /metrics for Prometheus scraping alongside the MCP endpoints.
app.MapPrometheusScrapingEndpoint();
app.MapMcp();
app.Run();
