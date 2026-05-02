// Chapter 6 — Section 6.1.4
// Contract test: compares the live tool descriptor returned by ListToolsAsync
// against a committed JSON snapshot. Fails if any previously required field has
// been removed from the live descriptor — catching breaking changes before clients
// are affected. Run SnapshotGenerator once to create the initial snapshot files.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Text.Json;
using Xunit;

namespace TravelBooking.Tests.Contract;

public class FlightsServerContractTests : IAsyncLifetime
{
    private McpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = "dotnet",
                Arguments = ["run", "--project",
                    "src/TravelBooking.FlightsServer", "--no-build"]
            }));
    }

    [Fact]
    public async Task SearchFlights_RequiredFields_MatchSnapshot()
    {
        using var snapshotDoc = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                "Snapshots/search_flights.json",
                TestContext.Current.CancellationToken));

        var tools = await _client.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var tool = tools.Single(t => t.Name == "search_flights");

        var snapshotRequired = snapshotDoc.RootElement
            .GetProperty("inputSchema").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        var liveRequired = tool.InputSchema
            .GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        // Fields in the snapshot that are absent from the live descriptor are breaking removals.
        // New fields in the live descriptor are additive and do not fail this assertion.
        Assert.Empty(snapshotRequired.Except(liveRequired));
    }

    [Fact]
    public async Task BookFlight_RequiredFields_MatchSnapshot()
    {
        using var snapshotDoc = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                "Snapshots/book_flight.json",
                TestContext.Current.CancellationToken));

        var tools = await _client.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var tool = tools.Single(t => t.Name == "book_flight");

        var snapshotRequired = snapshotDoc.RootElement
            .GetProperty("inputSchema").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        var liveRequired = tool.InputSchema
            .GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        Assert.Empty(snapshotRequired.Except(liveRequired));
    }

    [Fact]
    public async Task AllExpectedTools_AreRegistered()
    {
        var tools = await _client.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var names = tools.Select(t => t.Name).ToHashSet();
        Assert.Contains("search_flights", names);
        Assert.Contains("book_flight", names);
        Assert.Contains("cancel_flight", names);
    }

    [Fact]
    public async Task AllTools_HaveNonEmptyDescriptions()
    {
        var tools = await _client.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        foreach (var tool in tools)
            Assert.False(string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' is missing a description.");
    }

    public async Task DisposeAsync() => await _client.DisposeAsync();
}

// Snapshot generation utility — run once per server to capture initial descriptors.
// Re-run and commit snapshot files whenever an intentional breaking change is made.
public static class SnapshotGenerator
{
    public static async Task GenerateAsync(
        McpClient client, string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var tools = await client.ListToolsAsync(
            cancellationToken: cancellationToken);

        Directory.CreateDirectory(outputDirectory);

        foreach (var tool in tools)
        {
            var snapshot = new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = tool.InputSchema
            };

            var json = JsonSerializer.Serialize(snapshot,
                new JsonSerializerOptions { WriteIndented = true });

            var path = Path.Combine(outputDirectory, $"{tool.Name}.json");
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
