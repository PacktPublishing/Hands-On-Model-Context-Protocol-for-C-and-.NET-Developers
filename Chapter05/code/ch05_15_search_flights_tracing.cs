// Chapter 5 — Section 5.3.5
// SearchFlights with an explicit ActivitySource span.
// Tags follow OpenTelemetry semantic conventions for travel domain attributes.
// The span is visible in Jaeger/Tempo traces as a child of the incoming HTTP request span.

using System.Diagnostics;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public sealed partial class FlightTools
{
    private static readonly ActivitySource _activitySource =
        new("TravelBooking.FlightsServer");

    [McpServerTool, Description("Search for available flights between two airports on a given date.")]
    public async Task<FlightSearchResult> SearchFlights(
        [Description("IATA origin airport code (e.g. LHR)")] string origin,
        [Description("IATA destination airport code (e.g. JFK)")] string destination,
        [Description("Departure date in ISO 8601 format (YYYY-MM-DD)")] string departureDate,
        [Description("Number of passengers (1-9)")] int passengerCount = 1,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SearchFlights");
        activity?.SetTag("flight.origin", origin);
        activity?.SetTag("flight.destination", destination);
        activity?.SetTag("flight.departure_date", departureDate);
        activity?.SetTag("flight.passenger_count", passengerCount);

        var result = await _searchService.SearchAsync(
            origin, destination, departureDate, passengerCount, cancellationToken);

        activity?.SetTag("flight.options_returned", result.Options.Count);
        return result;
    }
}
