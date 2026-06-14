// Chapter 12 — Section 12.1.2
// MCP server metrics registration using System.Diagnostics.Metrics.
// IMeterFactory is the DI-friendly alternative to new Meter() — it participates
// in the container lifetime and integrates with AddOpenTelemetry() configuration.
// Counter for invocation counts, Histogram for SLO latency distribution,
// ObservableGauge for current active SSE connections.

using System.Diagnostics.Metrics;

namespace TravelBooking.Flights.Telemetry;

public sealed class McpServerMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _invocations;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _latency;
    private static int _activeConnections;

    public McpServerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("TravelBooking.Flights");

        // Counter: only ever increases; monitoring system computes rate by differencing.
        _invocations = _meter.CreateCounter<long>(
            "mcp.tool.invocations",
            description: "Total tool invocations by tool name and outcome");

        _errors = _meter.CreateCounter<long>(
            "mcp.tool.errors",
            description: "Total tool invocations that returned an error result");

        // Histogram: captures distribution for percentile (p95, p99) SLO tracking.
        _latency = _meter.CreateHistogram<double>(
            "mcp.tool.duration_ms", unit: "ms",
            description: "Tool invocation latency distribution");

        // ObservableGauge: reports a snapshot on each collection cycle.
        // Volatile.Read ensures the value read is not stale due to CPU caching.
        _meter.CreateObservableGauge(
            "mcp.connections.active",
            () => Volatile.Read(ref _activeConnections),
            description: "Current active SSE connections");
    }

    public void RecordInvocation(string toolName, double latencyMs, bool isError)
    {
        var tags = new TagList
        {
            { "mcp.tool.name", toolName },
            { "status", isError ? "error" : "ok" }
        };
        _invocations.Add(1, tags);
        _latency.Record(latencyMs, tags);
        if (isError)
            _errors.Add(1, new TagList { { "mcp.tool.name", toolName } });
    }

    public static void ConnectionOpened()  => Interlocked.Increment(ref _activeConnections);
    public static void ConnectionClosed()  => Interlocked.Decrement(ref _activeConnections);

    public void Dispose() => _meter.Dispose();
}
