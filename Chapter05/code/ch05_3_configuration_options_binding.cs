// Chapter 5 — Section 5.1.3
// Typed options binding for AirlineOptions and ResilienceOptions.
// Secrets (API keys) are kept out of appsettings.json via user-secrets in development
// and environment variables or Key Vault in deployed environments.
//
// Run once per developer workstation:
//   dotnet user-secrets init
//   dotnet user-secrets set "Airline:ApiKey" "<your-key>"

using Microsoft.Extensions.Options;

// Bind strongly typed options classes to configuration sections
builder.Services.Configure<AirlineOptions>(
    builder.Configuration.GetSection("Airline"));
builder.Services.Configure<ResilienceOptions>(
    builder.Configuration.GetSection("Resilience"));

// -------------------------------------------------------------------
// Companion options records (placed in Options/ folder in full project)
// -------------------------------------------------------------------

public record AirlineOptions
{
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
    public int MaxRetries { get; init; } = 2;
}

public record ResilienceOptions
{
    public SearchResilienceOptions Search { get; init; } = new();
    public BookingResilienceOptions Booking { get; init; } = new();
}

public record SearchResilienceOptions
{
    public int TimeoutSeconds { get; init; } = 8;
    public int RetryCount { get; init; } = 2;
    public double CircuitBreakerThreshold { get; init; } = 0.5;
}

public record BookingResilienceOptions
{
    public int TimeoutSeconds { get; init; } = 15;
    public int RetryCount { get; init; } = 1;
    public double CircuitBreakerThreshold { get; init; } = 0.3;
}
