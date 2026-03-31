// Chapter 5 — Section 5.2.4
// Capability versioning: the current method carries no suffix; the previous version
// uses the _v1 suffix and delegates to the current method.
// The deprecation notice in the v1 description is the signal for clients to migrate.

using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed partial class FlightTools
{
    // Current version — adds cabin class filter introduced in v2
    [McpServerTool, Description(
        "Search for available flights between two airports on a given date. " +
        "Optionally filter by cabin class (economy, business, first).")]
    public async Task<FlightSearchResult> SearchFlights(
        [Description("IATA origin airport code (e.g. LHR)")] string origin,
        [Description("IATA destination airport code (e.g. JFK)")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [Description("Number of passengers (1-9)")] int passengerCount = 1,
        [Description("Cabin class: economy, business, or first")] string cabinClass = "economy",
        CancellationToken cancellationToken = default)
        => await _searchService.SearchAsync(
            origin, destination, departureDate, passengerCount, cabinClass, cancellationToken);

    // Previous version — retained for backwards compatibility; delegates to current
    [McpServerTool, Description(
        "Search for available flights (deprecated: use search_flights which adds cabin class filter).")]
    public Task<FlightSearchResult> SearchFlightsV1(
        [Description("IATA origin airport code")] string origin,
        [Description("IATA destination airport code")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [Description("Number of passengers (1-9)")] int passengerCount = 1,
        CancellationToken cancellationToken = default)
        => SearchFlights(
            origin, destination, departureDate, passengerCount,
            cancellationToken: cancellationToken);
}
