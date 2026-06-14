// Chapter 11 — Section 11.5.4
// Deployment health monitor for the soak period following a blue/green traffic shift.
// PeriodicTimer fires on a fixed schedule regardless of how long each health check takes,
// avoiding timer drift that occurs when Task.Delay restarts its countdown after each check.
// Returns true when the soak period completes without threshold violations; false signals rollback.

using Microsoft.Extensions.Logging;

namespace TravelBooking.Deployment;

public sealed class DeploymentHealthMonitor(
    HttpClient http,
    ILogger<DeploymentHealthMonitor> logger)
{
    public async Task<bool> MonitorSoakPeriodAsync(
        string healthEndpoint,
        TimeSpan soakDuration,
        TimeSpan checkInterval,
        HealthThresholds thresholds,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + soakDuration;

        // PeriodicTimer fires on a wall-clock schedule — no cumulative drift from check latency.
        using var timer = new PeriodicTimer(checkInterval);

        while (DateTimeOffset.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            var metrics = await CollectMetricsAsync(healthEndpoint, ct);

            if (metrics.ErrorRate > thresholds.MaxErrorRate)
            {
                logger.LogWarning(
                    "Error rate {Rate:P2} exceeds threshold {Max:P2} — triggering rollback",
                    metrics.ErrorRate, thresholds.MaxErrorRate);
                return false;
            }

            if (metrics.P95LatencyMs > thresholds.MaxP95LatencyMs)
            {
                logger.LogWarning(
                    "P95 latency {Latency}ms exceeds threshold {Max}ms — triggering rollback",
                    metrics.P95LatencyMs, thresholds.MaxP95LatencyMs);
                return false;
            }

            if (!metrics.HealthCheckPassed)
            {
                logger.LogWarning("Health check failed during soak period — triggering rollback");
                return false;
            }

            // WaitForNextTickAsync returns false when ct is cancelled.
            if (!await timer.WaitForNextTickAsync(ct))
                break;
        }

        logger.LogInformation("Soak period completed — deployment healthy");
        return true;
    }

    private async Task<DeploymentMetrics> CollectMetricsAsync(
        string endpoint, CancellationToken ct)
    {
        var healthPassed = false;
        try
        {
            var response = await http.GetAsync($"{endpoint}/health/ready", ct);
            healthPassed = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Health check request failed");
        }

        // In production, ErrorRate and P95LatencyMs come from Azure Monitor or Prometheus.
        // Replace with real metric queries before using in a pipeline.
        return new DeploymentMetrics(
            ErrorRate: 0.0,
            P95LatencyMs: 0,
            HealthCheckPassed: healthPassed);
    }
}

public record HealthThresholds(double MaxErrorRate, double MaxP95LatencyMs);
public record DeploymentMetrics(double ErrorRate, double P95LatencyMs, bool HealthCheckPassed);
