// Chapter 9 (Replacement) — Section 9.5.3
// SustainedErrorGuard: tracks consecutive failures per tool and raises EmergencyStopException
// when the threshold is crossed. RecordSuccess resets the counter so transient bursts
// do not permanently disable a tool that subsequently recovers.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace TravelBooking.Agentic;

public sealed class SustainedErrorGuard(
    int threshold = 3,
    ILogger<SustainedErrorGuard>? logger = null)
{
    private static readonly ActivitySource ActivitySource =
        new("TravelBooking.Agentic");

    private readonly ConcurrentDictionary<string, int> _failures = new();

    /// <summary>
    /// Call after each failed tool invocation.
    /// Throws <see cref="EmergencyStopException"/> when consecutive failures
    /// for <paramref name="toolName"/> reach <see cref="threshold"/>.
    /// </summary>
    public void RecordFailure(string toolName)
    {
        var count = _failures.AddOrUpdate(
            toolName, 1, (_, current) => current + 1);

        logger?.LogWarning(
            "Tool '{Tool}' consecutive failure count: {Count}/{Threshold}",
            toolName, count, threshold);

        if (count >= threshold)
        {
            using var activity = ActivitySource.StartActivity("workflow.emergency_stop");
            activity?.SetTag("tool", toolName);
            activity?.SetTag("failures", count);
            activity?.SetStatus(ActivityStatusCode.Error);

            throw new EmergencyStopException(
                $"Tool '{toolName}' failed {count} consecutive times. " +
                "Halting workflow to prevent further side effects.");
        }
    }

    /// <summary>
    /// Call after each successful tool invocation to reset the counter.
    /// </summary>
    public void RecordSuccess(string toolName) =>
        _failures.TryRemove(toolName, out _);

    /// <summary>
    /// Returns the current consecutive failure count for a tool (0 if none).
    /// </summary>
    public int ConsecutiveFailures(string toolName) =>
        _failures.GetValueOrDefault(toolName, 0);
}

public sealed class EmergencyStopException(string message) : Exception(message);
