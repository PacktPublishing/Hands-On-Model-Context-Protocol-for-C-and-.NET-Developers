// Chapter 8 — Section 8.4.1
// Orchestrator metrics using System.Diagnostics.Metrics.
// Exports to any OpenTelemetry-compatible backend via AddOpenTelemetryMetrics().
// RecordWorkflow is called once per completed planning loop with the outcome and iteration count.

using System.Diagnostics.Metrics;

namespace TravelBooking.Orchestration;

public sealed class OrchestratorMetrics : IDisposable
{
    private readonly Meter _meter = new("TravelBooking.Orchestrator");

    private readonly Counter<int> _completions;
    private readonly Histogram<int> _iterations;
    private readonly Histogram<double> _latencyMs;
    private readonly Counter<int> _hallucinations;

    public OrchestratorMetrics()
    {
        _completions = _meter.CreateCounter<int>(
            "orchestrator.completions",
            description: "Number of completed orchestrator workflows");

        _iterations = _meter.CreateHistogram<int>(
            "orchestrator.iterations",
            description: "ReAct loop iterations per completed workflow");

        _latencyMs = _meter.CreateHistogram<double>(
            "orchestrator.latency_ms",
            unit: "ms",
            description: "End-to-end wall-clock time from user input to final response");

        _hallucinations = _meter.CreateCounter<int>(
            "orchestrator.hallucinations",
            description: "Tool calls with a name absent from the server capability list");
    }

    public void RecordWorkflow(bool success, int iterations, double latencyMs)
    {
        _completions.Add(1,
            new KeyValuePair<string, object?>("success", success));
        _iterations.Record(iterations,
            new KeyValuePair<string, object?>("success", success));
        _latencyMs.Record(latencyMs,
            new KeyValuePair<string, object?>("success", success));
    }

    public void RecordHallucination(string attemptedToolName)
    {
        _hallucinations.Add(1,
            new KeyValuePair<string, object?>("tool_name", attemptedToolName));
    }

    public void Dispose() => _meter.Dispose();
}
