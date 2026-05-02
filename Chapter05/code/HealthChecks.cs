// Chapter 5 — Health checks and capability validation hosted service.
// Consolidates the patterns from ch05_4 (readiness probe) and ch05_8 (startup validation).

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace TravelBooking.Chapter05;

/// <summary>
/// Readiness probe for the airline partner API.
/// In dev the configured base URL points at the RFC 2606 reserved <c>example.com</c>
/// host, so we treat that as a mock environment and return Healthy without dialling out.
/// </summary>
public sealed class AirlineApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AirlineOptions _options;

    public AirlineApiHealthCheck(IHttpClientFactory httpClientFactory, IOptions<AirlineOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Mock mode: the default base URL is a reserved example domain — skip the network call.
        if (Uri.TryCreate(_options.ApiBaseUrl, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith(".example.com", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Healthy("Mock airline API (example.com); no network call performed.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AirlineApi");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            using var response = await client.GetAsync("/health", cts.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Airline API is reachable")
                : HealthCheckResult.Degraded($"Airline API returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Airline API is unreachable", ex);
        }
    }
}

/// <summary>
/// Startup validation: every <see cref="McpServerToolAttribute"/> method must carry a
/// non-empty <see cref="DescriptionAttribute"/>. Throws at startup so misconfiguration
/// is caught before any client connects.
/// </summary>
public sealed class CapabilityValidationService : IHostedService
{
    private readonly ILogger<CapabilityValidationService> _logger;

    public CapabilityValidationService(ILogger<CapabilityValidationService> logger)
        => _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var toolTypes = Assembly.GetEntryAssembly()!
            .GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);

        foreach (var type in toolTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

            foreach (var method in methods)
            {
                var desc = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrWhiteSpace(desc))
                    throw new InvalidOperationException(
                        $"Tool method '{type.Name}.{method.Name}' is missing a [Description]. " +
                        "All tools must describe their purpose for LLM discoverability.");

                _logger.LogInformation("Validated tool: {Type}.{Method}", type.Name, method.Name);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
