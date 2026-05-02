// Chapter 6 — Section 6.3.3 / 6.3.4
// Chaos tests: verify that Polly timeout, retry, circuit breaker, concurrency limiter,
// and partial failure isolation policies all fire exactly as configured.
// Each test mutates FaultOptions to activate the relevant failure mode, then asserts
// on the observable side effect — error response, elapsed time, or healthy sibling tool.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Diagnostics;
using TravelBooking.FlightsServer.Chaos;
using Xunit;

namespace TravelBooking.Tests.Chaos;

public class FlightsServerChaosTests : IAsyncLifetime
{
    private McpClient _client = null!;
    private readonly FaultOptions _faultOptions = new();

    public async Task InitializeAsync()
    {
        _client = await McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = "dotnet",
                Arguments = ["run", "--project",
                    "src/TravelBooking.FlightsServer",
                    "--no-build",
                    "--environment", "Staging"]
            }));
    }

    // Timeout experiment: airline API delayed 12 s; Polly timeout policy fires at 8 s.
    // Two assertions confirm both that the policy fired (IsError) and when (elapsed < 10 s).
    [Fact]
    public async Task SearchFlights_DelayExceedsTimeout_ReturnsErrorWithinDeadline()
    {
        _faultOptions.DelayMs = 12_000;

        var stopwatch = Stopwatch.StartNew();
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);
        stopwatch.Stop();

        Assert.True(result.IsError == true);
        // 10-second ceiling gives 2 s buffer above the 8 s policy to absorb retry overhead.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10));
    }

    // Retry experiment: 100% error rate exhausts all retries; tool error returned to client.
    [Fact]
    public async Task SearchFlights_AllRetriesFail_ReturnsToolError()
    {
        _faultOptions.ErrorRate = 1.0;

        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError == true);
        Assert.NotEmpty(result.Content);
    }

    // Partial failure isolation: search path fails while cancel_flight stays healthy.
    // If both capabilities share one named HttpClient, this test fails and reveals the bug.
    [Fact]
    public async Task CancelFlight_HealthyWhenSearchPathFails_ReturnsConfirmation()
    {
        _faultOptions.ErrorRate = 1.0;

        var cancelResult = await _client.CallToolAsync(
            "cancel_flight",
            new Dictionary<string, object?>
            {
                ["bookingReference"] = "B-20250615-001",
                ["reason"] = "Schedule change"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(cancelResult.IsError);
    }

    // Circuit breaker experiment: sustained errors should eventually open the circuit,
    // causing subsequent calls to fast-fail without hitting the airline API.
    [Fact]
    public async Task SearchFlights_SustainedErrors_CircuitOpensAndFastFails()
    {
        _faultOptions.ErrorRate = 1.0;

        // Exhaust enough calls to trip the circuit breaker threshold.
        for (var i = 0; i < 10; i++)
        {
            await _client.CallToolAsync(
                "search_flights",
                new Dictionary<string, object?>
                {
                    ["origin"] = "LHR", ["destination"] = "AMS",
                    ["departureDate"] = "2025-06-15"
                },
                cancellationToken: TestContext.Current.CancellationToken);
        }

        // Once the circuit is open, calls should fast-fail well under the timeout period.
        var stopwatch = Stopwatch.StartNew();
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);
        stopwatch.Stop();

        Assert.True(result.IsError == true);
        // Fast-fail should complete well under 1 second, not the full 8 s timeout.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
    }

    public async Task DisposeAsync()
    {
        _faultOptions.Reset();
        await _client.DisposeAsync();
    }
}
