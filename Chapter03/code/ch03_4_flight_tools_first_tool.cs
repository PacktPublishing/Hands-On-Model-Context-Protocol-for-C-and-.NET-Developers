// Chapter 3 — Section 3.2.4
// First tool: FlightTools class with a SearchFlights stub registered via [McpServerToolType].

using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class FlightTools
{
    [McpServerTool]
    [Description("Search for available flights between two airports on a given date.")]
    public static FlightSearchResult SearchFlights(
        [Description("IATA origin airport code, e.g. LHR.")] string origin,
        [Description("IATA destination airport code, e.g. JFK.")] string destination,
        [Description("Departure date in ISO 8601 format, e.g. 2026-06-15.")] string date)
    {
        // Stub: real implementation in Chapter 5.
        return new FlightSearchResult(origin, destination, date, []);
    }
}

public record FlightSearchResult(string Origin, string Destination, string Date, object[] Flights);
