// Chapter 11 — Section 11.1.4
// Health check endpoints wired to three probe types.
// Liveness reports whether the process is alive; failure causes the platform to kill and restart the container.
// Readiness checks downstream dependencies; failure removes the replica from the load balancer.
// Startup gives the server a longer grace period on first boot before liveness kicks in.

using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    // The self check is tagged "live" only — it never queries external services.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    // Readiness checks verify dependencies before the server accepts traffic.
    .AddCheck<AirlineApiHealthCheck>("airline-api", tags: ["ready"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    // Startup check is also included in readiness so the cluster waits for full initialization.
    .AddCheck<McpSdkHealthCheck>("mcp-sdk", tags: ["ready", "startup"]);

var app = builder.Build();

// Liveness: kill and restart on failure
app.MapHealthChecks("/health/live", new()
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness: remove from load balancer on failure
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

// Startup: used only during initialisation; longer failure tolerance than liveness
app.MapHealthChecks("/health/startup", new()
{
    Predicate = check => check.Tags.Contains("startup")
});

app.Run();

// Readiness check: confirms the upstream airline API is reachable before accepting MCP tool calls.
public sealed class AirlineApiHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient("airline-api");
            var response = await http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Airline API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Airline API unreachable.", ex);
        }
    }
}

// Stub implementations required by the health check registrations above.
public sealed class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(HealthCheckResult.Healthy());
}

public sealed class McpSdkHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(HealthCheckResult.Healthy());
}
