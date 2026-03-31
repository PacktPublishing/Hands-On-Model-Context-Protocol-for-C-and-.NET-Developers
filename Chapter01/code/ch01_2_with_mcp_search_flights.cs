// Chapter 1 — Section 1.4
// SearchFlightsTool registered with MCP: discovered automatically by any MCP-compliant host.
// The SDK generates the JSON Schema from C# types and Description attributes at startup.
// Full server setup and DI registration is in the companion repository under src/FlightsServer/.

using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

/// <summary>
/// After MCP: capabilities are declared once and consumed by any compliant host.
/// No bespoke adapter per consumer. Schema is generated automatically. Auth is handled
/// by the transport layer (Chapter 10), not by the tool itself.
/// </summary>
[McpServerToolType]
public class SearchFlightsTool(IFlightSearchService flights)
{
    [McpServerTool, Description("Search available flights between two airports on a given date.")]
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
        return await flights.SearchAsync(origin, destination, departureDate, ct);
    }
}
