// Chapter 9 — Section 9.4.2
// Client-side UX performance metrics using System.Diagnostics.Metrics.
// Exported through the same OpenTelemetry pipeline as server-side metrics (Chapter 5)
// so client and server latency appear side by side on the same dashboard.
// Record TimeToFirstToken when the first non-null update.Text arrives in the streaming loop.

using System.Diagnostics.Metrics;

namespace TravelBooking.Blazor.Services;

public static class UxMetrics
{
    private static readonly Meter Meter = new("TravelBooking.UX");

    // Wall-clock time from button click to first rendered token (ms).
    public static readonly Histogram<double> TimeToFirstToken =
        Meter.CreateHistogram<double>(
            "ux.time_to_first_token_ms",
            unit: "ms",
            description: "Time from search submission to first LLM token rendered");

    // Wall-clock time from button click to last rendered token (ms).
    public static readonly Histogram<double> TimeToComplete =
        Meter.CreateHistogram<double>(
            "ux.time_to_complete_ms",
            unit: "ms",
            description: "Total streaming duration from submission to final token");

    // Incremented in every component catch block that calls McpErrorHandler.Classify().
    public static readonly Counter<int> ErrorCount =
        Meter.CreateCounter<int>(
            "ux.errors",
            description: "Number of errors surfaced to the user in the UI");

    // Live gauge of jobs waiting in the McpJobQueue channel.
    public static readonly ObservableGauge<int> JobQueueDepth =
        Meter.CreateObservableGauge<int>(
            "ux.job_queue_depth",
            () => JobQueueSnapshot.PendingCount,
            description: "Current number of pending background jobs in the queue");
}

// Updated by JobStatusStore whenever a job transitions to Running or Completed/Failed.
public static class JobQueueSnapshot
{
    private static int _pendingCount;
    public static int PendingCount => _pendingCount;

    public static void Increment() =>
        System.Threading.Interlocked.Increment(ref _pendingCount);

    public static void Decrement() =>
        System.Threading.Interlocked.Decrement(ref _pendingCount);
}
