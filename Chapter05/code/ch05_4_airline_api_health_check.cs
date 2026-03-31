// Chapter 5 — Section 5.1.4
// Readiness probe for the airline partner API.
// Registered with tag "ready" so it only participates in the /health/ready endpoint,
// not the /health/live liveness check that just confirms the process is running.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

public sealed class AirlineApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AirlineOptions _options;

    public AirlineApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<AirlineOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AirlineApi");
            using var response = await client.GetAsync(
                "/health", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Airline API is reachable")
                : HealthCheckResult.Degraded(
                    $"Airline API returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Airline API is unreachable", ex);
        }
    }
}
