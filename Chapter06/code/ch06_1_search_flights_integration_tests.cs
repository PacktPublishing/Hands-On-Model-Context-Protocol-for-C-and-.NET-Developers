// Chapter 6 — Section 6.1.3
// Integration test class for the SearchFlights tool using McpClient.CreateAsync.
// StdioClientTransport launches the FlightsServer as a subprocess, exercising the
// full JSON-RPC serialisation, schema validation, and handler dispatch path.
// result.IsError is bool? — null = success, true = tool error. Never assert False.

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Xunit;

namespace TravelBooking.Tests.Integration;

public class SearchFlightsIntegrationTests : IAsyncLifetime
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
    public async Task SearchFlights_ValidRoute_ReturnsResults()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.IsError);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task SearchFlights_InvalidIataCode_ReturnsToolError()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "INVALID", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError == true);
        var errorText = result.Content.First().Text;
        Assert.Contains("IATA", errorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchFlights_PastDate_ReturnsToolError()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2020-01-01"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError == true);
        var errorText = result.Content.First().Text;
        Assert.Contains("future", errorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchFlights_MaxPassengerCount_ReturnsResults()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15",
                ["passengerCount"] = 9
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.IsError);
    }

    [Fact]
    public async Task SearchFlights_ZeroPassengers_ReturnsToolError()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "AMS",
                ["departureDate"] = "2025-06-15",
                ["passengerCount"] = 0
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError == true);
    }

    [Fact]
    public async Task SearchFlights_SameOriginAndDestination_ReturnsToolError()
    {
        var result = await _client.CallToolAsync(
            "search_flights",
            new Dictionary<string, object?>
            {
                ["origin"] = "LHR", ["destination"] = "LHR",
                ["departureDate"] = "2025-06-15"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError == true);
    }

    public async Task DisposeAsync() => await _client.DisposeAsync();
}
