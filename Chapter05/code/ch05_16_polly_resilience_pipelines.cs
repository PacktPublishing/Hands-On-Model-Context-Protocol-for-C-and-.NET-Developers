// Chapter 5 — Section 5.4.1
// Named Polly v8 resilience pipelines for search and booking.
// Search: higher concurrency (20), 2 retries with jitter, 50% circuit breaker threshold.
// Booking: tighter limits (10), only 1 retry (duplicate bookings are costly), 30% threshold.
// Inject via ResiliencePipelineProvider<string> in tool handlers.

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

builder.Services.AddResiliencePipeline("flights-search", pipeline => pipeline
    .AddConcurrencyLimiter(permitLimit: 20, queuedTasksLimit: 50)
    .AddTimeout(TimeSpan.FromSeconds(8))
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 2,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15)
    }));

builder.Services.AddResiliencePipeline("flights-booking", pipeline => pipeline
    .AddConcurrencyLimiter(permitLimit: 10, queuedTasksLimit: 20)
    .AddTimeout(TimeSpan.FromSeconds(15))
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 1,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.3,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(30)
    }));
