// Chapter 4 — Section 4.4.4
// SLA acceptance test for SearchFlightsTool.
// Asserts that the p95 observed latency across 1000 invocations remains below the configured target.
//
// Prerequisites:
//   - Build FlightsServer from Chapter 5 before running this test.
//   - Run in a dedicated CI stage separate from unit tests (requires a real network process).
//
// Usage:
//   dotnet test Chapter04/tests/Chapter04.Tests.csproj

using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace TravelBooking.Chapter04.Tests;

public class FlightSearchSlaTests
{
    // Illustrative target — replace with the value measured from your FlightsServer deployment.
    private const double P95TargetMs = 300.0;
    private const int Runs = 1000;

    [Fact]
    public async Task SearchFlightsTool_meetsP95LatencyTarget()
    {
        var latencies = new List<double>(Runs);

        await using var client = await CreateFlightsMcpClientAsync();

        for (int i = 0; i < Runs; i++)
        {
            var sw = Stopwatch.StartNew();
            await client.CallToolAsync(
                "SearchFlightsTool",
                new Dictionary<string, object?>
                {
                    ["origin"]      = "LHR",
                    ["destination"] = "JFK",
                    ["date"]        = "2026-06-15"
                });
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        // Sort ascending: index (Runs * 0.95 - 1) is the 95th percentile observation.
        latencies.Sort();
        var p95Index = (int)Math.Ceiling(Runs * 0.95) - 1;
        var p95      = latencies[p95Index];
        var p50      = latencies[Runs / 2 - 1];
        var p99      = latencies[(int)Math.Ceiling(Runs * 0.99) - 1];

        Assert.True(
            p95 < P95TargetMs,
            $"SearchFlightsTool p95 latency {p95:F1}ms exceeds SLA target {P95TargetMs}ms. " +
            $"p50={p50:F1}ms  p99={p99:F1}ms");
    }

    // Builds a client connected to FlightsServer via stdio transport.
    // The FlightsServer project path must be built before running this test.
    // Update the Arguments value to match the built FlightsServer binary location in your repo.
    private static async Task<McpClient> CreateFlightsMcpClientAsync()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name      = "FlightsServer",
            Command   = "dotnet",
            Arguments = ["run", "--project", "../../Chapter05/src/FlightsServer", "--no-build"]
        });

        return await McpClient.CreateAsync(transport);
    }
}
