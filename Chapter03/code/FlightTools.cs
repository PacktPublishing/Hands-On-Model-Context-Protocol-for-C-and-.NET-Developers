// Chapter 3 - Flight Search Tool
// Demonstrates MCP tool registration that works with both stdio and HTTP transports
// The same tool implementation can be used regardless of transport choice

using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.Chapter03;

/// <summary>
/// Flight search tool for MCP server demonstrations
/// This single implementation works with both stdio and HTTP transports
/// </summary>
[McpServerToolType]
public class FlightTools(IFlightSearchService flightSearch)
{
    [McpServerTool]
    [Description("Search available flights between two airports on a given date.")]
    public async Task<FlightSearchResult> SearchFlightsAsync(
        [Description("IATA origin airport code, e.g. LHR for London Heathrow.")]
        string origin,

        [Description("IATA destination airport code, e.g. JFK for New York.")]
        string destination,

        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")]
        string date,

        CancellationToken ct = default)
    {
        var departureDate = DateOnly.Parse(date);
        return await flightSearch.SearchAsync(origin, destination, departureDate, ct);
    }
}
