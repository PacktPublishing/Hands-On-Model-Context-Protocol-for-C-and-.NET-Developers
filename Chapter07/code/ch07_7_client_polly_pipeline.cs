// Chapter 7 — Section 7.4.1 / 7.4.2 / 7.4.3
// Client-side Polly v8 resilience pipeline wrapping CallToolAsync.
// Pipeline layers from outermost to innermost:
//   Timeout → CircuitBreaker → Retry → ConcurrencyLimiter → FlightsServer
// ShouldHandle excludes TransportClosedException (requires new McpClient, not retry).
// Fallback is applied only to idempotent read operations (search_flights).
// Never apply a success fallback to write operations (book_flight, cancel_flight).

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Client.Resilience;

public static class ClientResilienceExtensions
{
    public static IServiceCollection AddFlightsClientPipeline(
        this IServiceCollection services)
    {
        services.AddResiliencePipeline("flights-client", pipeline =>
        {
            pipeline
                .AddTimeout(TimeSpan.FromSeconds(8))
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15)
                })
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<McpException>()
                        .Handle<HttpRequestException>()
                        // TransportClosedException is NOT retriable — it requires a new McpClient.
                })
                .AddConcurrencyLimiter(permitLimit: 20, queuedTasksLimit: 50);
        });

        return services;
    }
}

// Resilient client wrapper — resolves the pipeline once at construction.
// Builds the pipeline from DI rather than constructing it per-call,
// which would reset the circuit-breaker state on every invocation.
public sealed class ResilientFlightClient
{
    private readonly McpClient _client;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ResilientFlightClient> _logger;
    private readonly IMemoryCache _cache;

    public ResilientFlightClient(
        McpClient client,
        ResiliencePipelineProvider<string> pipelineProvider,
        IMemoryCache cache,
        ILogger<ResilientFlightClient> logger)
    {
        _client = client;
        _pipeline = pipelineProvider.GetPipeline("flights-client");
        _cache = cache;
        _logger = logger;
    }

    // Search: idempotent read — fallback to cache when circuit is open.
    public async ValueTask<CallToolResult> SearchFlightsAsync(
        string origin, string destination, string date,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"search:{origin}:{destination}:{date}";
        try
        {
            var result = await _pipeline.ExecuteAsync(ct =>
                _client.CallToolAsync("search_flights",
                    new Dictionary<string, object?>
                    {
                        ["origin"] = origin,
                        ["destination"] = destination,
                        ["departureDate"] = date
                    },
                    cancellationToken: ct),
                cancellationToken);

            // Cache successful results for fallback use.
            if (result.IsError != true)
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit open for search_flights. Returning cached results.");
            if (_cache.TryGetValue<CallToolResult>(cacheKey, out var cached) && cached is not null)
                return cached;

            throw;
        }
    }

    // Book: write operation — never apply a success fallback.
    // Let BrokenCircuitException propagate to the workflow coordinator.
    public ValueTask<CallToolResult> BookFlightAsync(
        string flightId, string idempotencyKey, string passengerName,
        CancellationToken cancellationToken = default)
        => _pipeline.ExecuteAsync(ct =>
            _client.CallToolAsync("book_flight",
                new Dictionary<string, object?>
                {
                    ["flightId"] = flightId,
                    ["idempotencyKey"] = idempotencyKey,
                    ["passengerName"] = passengerName
                },
                cancellationToken: ct),
            cancellationToken);
}
