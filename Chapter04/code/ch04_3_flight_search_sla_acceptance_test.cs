// Chapter 4 — Section 4.4.4
// SLA acceptance test for SearchFlightsTool.
// Asserts that the p95 observed latency across 1000 invocations remains below 300ms.
// Run this in a dedicated CI stage against a fully deployed FlightsServer instance.

using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

public class FlightSearchSlaTests
{
    [Fact]
    public async Task SearchFlightsTool_meetsP95LatencyTarget()
    {
        const int runs = 1000;
        const double p95TargetMs = 300.0;
        var latencies = new List<double>(runs);

        await using var client = await CreateFlightsMcpClientAsync();

        for (int i = 0; i < runs; i++)
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
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        // Sort ascending so index (runs * 0.95 - 1) is the 95th percentile observation.
        latencies.Sort();
        var p95Index = (int)Math.Ceiling(runs * 0.95) - 1;
        var p95 = latencies[p95Index];

        Assert.True(
            p95 < p95TargetMs,
            $"SearchFlightsTool p95 latency {p95:F1}ms exceeds SLA target {p95TargetMs}ms. " +
            $"p50={latencies[runs / 2 - 1]:F1}ms, p99={latencies[(int)Math.Ceiling(runs * 0.99) - 1]:F1}ms.");
    }

    private static async Task<McpClient> CreateFlightsMcpClientAsync()
    {
        // Points at the FlightsServer project via stdio transport for local and CI execution.
        // Replace the command path with your built FlightsServer binary path in CI configuration.
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name      = "FlightsServer",
            Command   = "dotnet",
            Arguments = ["run", "--project", "src/FlightsMcpServer", "--no-build"]
        });

        return await McpClient.CreateAsync(transport);
    }
}
