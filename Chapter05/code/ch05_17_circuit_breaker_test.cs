// Chapter 5 — Section 5.4.5
// xUnit test verifying the booking circuit breaker opens after threshold failures.
// Uses a FakeAirlineClient (failure mode on) to count actual calls.
// After enough failures the circuit opens; the next call is rejected immediately
// with BrokenCircuitException before reaching the fake client.

using Polly;
using Polly.CircuitBreaker;
using Xunit;

public class CircuitBreakerTests
{
    [Fact]
    public async Task BookingPipeline_OpensCircuitAfterThresholdFailures()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(2)
            })
            .Build();

        var fakeAirline = new FakeAirlineClient(failureMode: true);

        // Drive failures until the circuit opens
        for (int i = 0; i < 5; i++)
        {
            try { await pipeline.ExecuteAsync(_ => fakeAirline.BookAsync("F001")); }
            catch { /* expected */ }
        }

        // Circuit is open — the next call must be rejected without reaching the client
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await pipeline.ExecuteAsync(_ => fakeAirline.BookAsync("F001")));

        // Only 5 calls reached the airline; the 6th was short-circuited
        Assert.Equal(5, fakeAirline.CallCount);
    }
}
