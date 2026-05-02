// Chapter 6 — Section 6.3.2
// FaultInjectionHandler: a DelegatingHandler that intercepts outbound HTTP calls
// and introduces configurable delays or HTTP 503 errors before forwarding.
// Register exclusively in staging/test DI via environment-conditional branching.
// The cancellationToken parameter of Task.Delay is critical: it allows the Polly
// timeout policy to cancel an in-progress delay rather than waiting it out.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace TravelBooking.FlightsServer.Chaos;

public sealed class FaultOptions
{
    // Milliseconds to delay every outbound request. 0 = no delay injected.
    public int DelayMs { get; set; }

    // Fraction of requests that throw HttpRequestException(503) [0.0 – 1.0]. 0 = no errors.
    public double ErrorRate { get; set; }

    public void Reset()
    {
        DelayMs = 0;
        ErrorRate = 0;
    }
}

public class FaultInjectionHandler : DelegatingHandler
{
    private readonly FaultOptions _options;

    public FaultInjectionHandler(IOptions<FaultOptions> options)
        : base(new HttpClientHandler()) => _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_options.DelayMs > 0)
            await Task.Delay(_options.DelayMs, cancellationToken);

        if (_options.ErrorRate > 0 &&
            Random.Shared.NextDouble() < _options.ErrorRate)
            throw new HttpRequestException(
                "Simulated fault.",
                inner: null,
                statusCode: HttpStatusCode.ServiceUnavailable);

        return await base.SendAsync(request, cancellationToken);
    }
}

// Extension method — register fault injection for the named airline HTTP client.
// Apply only when ASPNETCORE_ENVIRONMENT is Staging to prevent accidental registration
// in Production. The IConfiguration binding reads chaos settings from appsettings.Staging.json.
public static class FaultInjectionExtensions
{
    public static IServiceCollection AddFaultInjection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FaultOptions>(
            configuration.GetSection("Chaos"));

        services.AddHttpClient("AirlineApi")
            .AddHttpMessageHandler<FaultInjectionHandler>();

        services.AddTransient<FaultInjectionHandler>();

        return services;
    }
}
