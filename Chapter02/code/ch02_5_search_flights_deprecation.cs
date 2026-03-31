// Chapter 2 - Section 2.4.3
// Deprecated SearchFlights_v1 tool alongside the current SearchFlights tool.
// The v1 variant maps the legacy "from" and "to" parameter names to the current "origin"
// and "destination" names, then delegates to the same underlying service.
// Both tools can coexist in the same [McpServerToolType] class during the transition window.

using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelBooking.CodeSamples.Shared;

namespace TravelBooking.CodeSamples;

[McpServerToolType]
public sealed class SearchFlightsWithDeprecation(IFlightSearchService flightSearch)
{
    /// <summary>Current tool: uses canonical parameter names "origin", "destination", "date".</summary>
    [McpServerTool, Description(
        "Search available flights between two airports on a given date. " +
        "Use IATA codes for airports, e.g. LHR or JFK.")]
    public async Task<FlightSearchResult> SearchFlightsAsync(
        [Description("IATA origin airport code, e.g. LHR.")]
        string origin,

        [Description("IATA destination airport code, e.g. JFK.")]
        string destination,

        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")]
        string date,

        CancellationToken ct = default)
    {
        var departureDate = DateOnly.Parse(date);
        return await flightSearch.SearchAsync(origin, destination, departureDate, ct);
    }

    /// <summary>
    /// Legacy tool preserved for clients compiled against the v1 schema.
    /// Deprecated: use SearchFlights instead. This tool will be removed in a future release.
    /// </summary>
    [McpServerTool(Name = "SearchFlights_v1"), Description(
        "[DEPRECATED] Use SearchFlights instead. " +
        "This version accepts 'from' and 'to' instead of 'origin' and 'destination'. " +
        "It will be removed after 2026-12-01.")]
    public async Task<FlightSearchResult> SearchFlights_v1Async(
        [Description("IATA origin airport code (legacy name: 'from').")]
        string from,

        [Description("IATA destination airport code (legacy name: 'to').")]
        string to,

        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")]
        string date,

        CancellationToken ct = default)
    {
        // Delegate to the current implementation using the canonical parameter names.
        return await SearchFlightsAsync(
            origin: from,
            destination: to,
            date: date,
            ct: ct);
    }
}
